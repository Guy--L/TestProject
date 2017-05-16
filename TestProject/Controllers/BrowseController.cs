using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using TestProject.Models;
using System.Web;

namespace TestProject.Controllers
{
    [RoutePrefix("browse")]
    public class BrowseController : ApiController
    {
        // GET browse
        [HttpGet]
        public IEnumerable<Node> Get()
        {
            return Nodes.Children(0);
        }

        private IHttpActionResult Download(int id)
        {
            Node node = Nodes.Download(id);
            byte[] fileBytes = File.ReadAllBytes(node.path);
            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new ByteArrayContent(fileBytes);
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = node.filename
            };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return ResponseMessage(result);
        }

        // GET browse/5
        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult Get(int id)
        {
            if (Nodes.isDir(id))
                return Json(Nodes.Children(id));
            return Download(id);
        }

        [HttpGet]
        [Route("~/deep/{*path}")]
        public IHttpActionResult Deep(string path)
        {
            Node deep = Nodes.FromPath(path);
            if (deep.isFile)
                return Download(deep.id);
            return Json(Nodes.Children(deep.id));
        }

        // POST browse/5
        [HttpPost]
        [Route("{id}")]
        public IHttpActionResult Post(int id)
        {
            var httpRequest = HttpContext.Current.Request;
            var postedFile = httpRequest.Files[0];
            Nodes.Upload(id, postedFile);
            return Json(Nodes.Children(id));
        }

        // PUT fs/values/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        // DELETE browse/5
        [HttpDelete]
        [Route("{id}")]
        public IHttpActionResult Delete(int id)
        {
            var parentid = Nodes.Delete(id);
            return Json(Nodes.Children(parentid));
        }
    }
}