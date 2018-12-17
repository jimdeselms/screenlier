using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Screenly.Server.Data;
using Screenly.Server.Models;

namespace server.Controllers
{
    [Route("images")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ITestRepository _repository;

        public ImageController()
        {
            _repository = RepositoryFactory.GetTestRepository();
            _repository.EnsureSchema();
        }

        [HttpGet("benchmark/{app}/{*path}")]
        public ActionResult GetBenchmark(string app, string path)
        {
            HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

            var benchmark = _repository.GetBenchmark(app, path);
            if (benchmark == null)
            {
                 return NotFound();
            }
            else
            {
                return File(benchmark, "image/png");
            }
        }

        [HttpGet("refimage/{testRunId}/{*path}")]
        public ActionResult GetReferenceImage(int testRunId, string path)
        {
            HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

            var refImage = _repository.GetReferenceImage(testRunId, path);
            if (refImage == null)
            {
                 return NotFound();
            }
            else
            {
                return File(refImage, "image/png");
            }
        }

        [HttpGet("testimage/{testRunId}/{*path}")]
        public ActionResult GetTestImage(int testRunId, string path)
        {
            HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

            var testImage = _repository.GetTestImage(testRunId, path);
            if (testImage == null)
            {
                 return NotFound();
            }
            else
            {
                return File(testImage, "image/png");
            }
        }

        [HttpGet("diffimage/{testRunId}/{*path}")]
        public ActionResult GetTestDifference(int testRunId, string path)
        {
            HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

            var diffImage = _repository.GetTestDifference(testRunId, path);
            if (diffImage == null)
            {
                 return NotFound();
            }
            else
            {
                return File(diffImage, "image/png");
            }
        }
    }
}
