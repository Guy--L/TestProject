using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace TestProject.Models
{
    public class Nodes
    {
        static object semaphore = new object();
        static string _root;
        static ConcurrentDictionary<int, Node> _nodes;
        static ConcurrentDictionary<string, int> _paths;

        private static string subpath(string path) {
            return path.Substring(_root.Length);
        }

        /// <summary>
        /// Construct Nodes service
        /// </summary>
        /// <param name="string">Underlying absolute path on physical file system</param>
        public Nodes(string root)
        {
            _root = root;
            _nodes = new ConcurrentDictionary<int, Node>();
            _paths = new ConcurrentDictionary<string, int>();             // support deep linking

            var rootnode = Cache(new Node(_root, 0, false));    // set the root
            ReadChildren(rootnode);                             // cache in its children (and theirs)
        }

        /// <summary>
        /// Given n, add it to the global list with a unique id.
        /// Also add its subpath and new id to the a global index 
        /// </summary>
        /// <param name="n">Node to add</param>
        /// <returns>Node n</returns>
        private static Node Cache(Node n)
        {
            var newid = _nodes.Count;
            _nodes[newid] = n;
            _paths[subpath(n.path)] = newid;
            n.id = newid;
            return n;
        }

        /// <summary>
        /// Read contents of Node n from filesystem only if children not already set
        /// </summary>
        /// <param name="n">Node expected to be a directory</param>
        private static void ReadChildren(Node n)
        {
            if (n.isFile || n.children != null)
                return;

            List<Node> children = new List<Node>();
            foreach (var item in Directory.GetFileSystemEntries(n.path))
            {
                children.Add(Cache(new Node(n.id, item)));
            }
            n.children = children;
            return;
        }

        /// <summary>
        /// List children filesystem "nodes" and construct an uplink ".." entry 
        /// </summary>
        /// <param name="id">Unique Node Id</param>
        /// <returns>List of Children Nodes</returns>
        public static List<Node> Children(int id)
        {
            if (!_nodes.ContainsKey(id))
                return null;

            var node = _nodes[id];
            ReadChildren(node);

            node.children.All(c => { ReadChildren(c); return true; });      // need file counts for folders

            var upfirst = new List<Node>();
            var parent = _nodes[node.parentid];
            parent.subpath = '\\'+subpath(node.path)+'\\';
            upfirst.Add(parent);

            return upfirst.Concat(node.children).ToList();
        }

        public static bool isDir(int id)
        {
            if (!_nodes.ContainsKey(id))
                return false;
            return !_nodes[id].isFile;
        }

        public static Node Download(int id)
        {
            if (isDir(id))
                return null;

            return _nodes[id];
        }

        public static Node FromPath(string path)
        {
            var go = '\\'+path.Split('#')[0];

            if (go[go.Length - 1] == '/')
                go = go.Substring(0, go.Length - 1);

            var deep = go.Replace('/', '\\');
            if (!_paths.ContainsKey(deep))
                return null;

            return _nodes[_paths[deep]];
        }

        public static void Upload(int id, HttpPostedFile file)
        {
            if (!_nodes.ContainsKey(id))
                return;

            var node = _nodes[id];
            if (node.isFile)                            // upload next to sibling
                node = _nodes[node.parentid];

            var newpath = Path.Combine(node.path, Path.GetFileName(file.FileName));

            int i = 1;
            while (_paths.ContainsKey(subpath(newpath)))   // duplicate debounce
                newpath = newpath + " (" +(i++)+ ")";

            file.SaveAs(newpath);

            node.children.Add(Cache(new Node(newpath, id, file.ContentLength)));
        }

        /// <summary>
        /// Delete filesystem entries and corresponding nodes in sync
        /// </summary>
        /// <param name="n">Node to remove from Cache</param>
        public static void Prune(Node n)
        {
            Node gone;
            if (_nodes.TryRemove(n.id, out gone))
            {
                if (gone.isFile)
                    File.Delete(gone.path);
                else
                    Directory.Delete(gone.path, true);

                int index;
                _paths.TryRemove(subpath(n.path), out index);
            }

            if (n.children == null)
                return;

            foreach (var child in n.children)
                Prune(child);
        }

        /// <summary>
        /// Delete node and filesystem object, be it directory or file
        /// </summary>
        /// <param name="id">Node id to be deleted</param>
        /// <returns>Id of parent node</returns>
        public static int Delete(int id)
        {
            if (!_nodes.ContainsKey(id))
                return 0;

            Node parent;
            lock (semaphore)                // threaded transaction
            {
                var node = _nodes[id];

                parent = _nodes[node.parentid];     // parent reference 
                parent.children.Remove(node);       // would otherwise prevent removal

                Prune(node);                // remove filesystem items and their nodes
            }
            return parent.id;
        }
    }
}
