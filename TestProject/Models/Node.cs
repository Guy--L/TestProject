using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestProject.Models
{
    public class Node
    {
        public int id { get; set; }
        public int parentid { get; set; }
        public string filename { get; set; }
        public string subpath { get; set; }
        public bool isFile { get; set; }
        public long length { get; set; }
        public int? count { get { return children==null?(int?)null:children.Count(); } }
        [JsonIgnore] public string path { get; set; }
        [JsonIgnore] public List<Node> children { get; set; }

        public Node() { }

        /// <summary>
        /// Use filesystem to get length and file/directory status
        /// </summary>
        /// <param name="parent">Cached node id of parent node</param>
        /// <param name="item">full path name of node</param>
        public Node(int parent, string item)
        {
            var info = new FileInfo(item);
            filename = Path.GetFileName(item);
            path = item;
            isFile = info.Attributes != FileAttributes.Directory;
            length = isFile ? info.Length : 0;
            parentid = parent;
        }

        /// <summary>
        /// Special case constructor for root node
        /// </summary>
        /// <param name="newpath">full path</param>
        /// <param name="parent">parent node id</param>
        /// <param name="isaFile">root is a directory</param>
        public Node(string newpath, int parent = 0, bool isaFile = true)
        {
            filename = Path.GetFileName(newpath);
            path = newpath;
            isFile = isaFile;
            parentid = parent;
        }

        /// <summary>
        ///  Special case constructor for upload
        /// </summary>
        /// <param name="newpath">path saved to</param>
        /// <param name="parent">parent node id</param>
        /// <param name="len">file length</param>
        public Node(string newpath, int parent = 0, long len = 0)
        {
            filename = Path.GetFileName(newpath);
            path = newpath;
            isFile = true;
            length = len;
            parentid = parent;
        }
    }
}


