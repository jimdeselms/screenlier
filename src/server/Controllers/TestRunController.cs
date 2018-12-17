using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Screenly.Server.Data;
using Screenly.Server.Models;

namespace server.Controllers
{
    [Route("api/v1")]
    [EnableCors("AllowEverything")]    
    [ApiController]
    public class TestRunController : ControllerBase
    {
        private readonly ITestRepository _repository;

        public TestRunController()
        {
            _repository = RepositoryFactory.GetTestRepository();
            _repository.EnsureSchema();
        }

        [HttpGet("testrun/{id}")]
        public ActionResult<TestRun> Get(int id)
        {
            var result = _repository.GetTestRun(id);
            return result == null ? NotFound(null) : (ActionResult<TestRun>)result;
        }

        [HttpGet("testrun")]
        public ActionResult<IEnumerable<TestRunSummary>> GetTestRunSummaries(string appname)
        {
            return _repository.GetTestRunSummaries(appname).ToArray();
        }

        [HttpPost("testrun/{app}")]
        public int CreateTestRun(string app)
        {
            return _repository.CreateTestRun(app);
        }

        [HttpPatch("testrun/{testRunId}")]
        public void SetTestRunEnd(int testRunId, bool complete)
        {
            if (complete)
            {
                _repository.SetTestRunEnd(testRunId, DateTime.Now);
            }
        }

        [HttpPut("benchmark/{app}/{*path}")]
        public void PutBenchmark(string app, string path, string name)
        {
            var benchmark = GetRawBody();
            _repository.SaveBenchmark(app, path, name, benchmark);
        }

        [HttpPut("refimage/{testRunId}/{*path}")]
        public void PutReferenceImage(int testRunId, string path, string name)
        {
            var image = GetRawBody();
            _repository.SaveReferenceImage(testRunId, path, name, image);
        }

        [HttpPut("testimage/{testRunId}/{*path}")]
        public void PutTestImage(int testRunId, string path, string name)
        {
            var image = GetRawBody();
            _repository.SaveTestImage(testRunId, path, name, image);
        }

        [HttpPut("testimage/promote/{testRunId}/{*path}")]
        public void PromoteTestImage(int testRunId, string path)
        {
            _repository.PromoteBenchmark(testRunId, path);
        }

        [HttpPut("testimageerror/{testRunId}/{*path}")]
        public void PutTestImageError(int testRunId, string path, string name)
        {
            var error = System.Text.Encoding.UTF8.GetString(GetRawBody());
            _repository.SaveTestImageError(testRunId, path, name, error);
        }

        [HttpPost("testimage/imageclaim/{claimedBy}")]
        public ActionResult<ClaimedTestImageInfo> ClaimTestImage(string claimedBy)
        {
            return _repository.ClaimNextTestImage(claimedBy);
        }

        [HttpPost("testimage/success/{testRunId}/{*path}")]
        public void MarkTestImageSuccess(int testRunId, string path)
        {
            _repository.MarkTestImageSuccess(testRunId, path);
        }

        [HttpPost("testimage/different/{testRunId}/{*path}")]
        public void MarkTestImageDifferent(int testRunId, string path)
        {
            var difference = GetRawBody();
            _repository.MarkTestImageDifferent(testRunId, path, difference);
        }

        [HttpPost("testimage/error/{testRunId}/{*path}")]
        public void MarkTestImageError(int testRunId, string path)
        {
            var error = System.Text.Encoding.UTF8.GetString(GetRawBody());
            _repository.MarkTestImageError(testRunId, path, error);
        }

        private byte[] GetRawBody()
        {
            using (var reader = new BinaryReader(Request.Body))
            {
                return reader.ReadBytes((int)10000000);
            }
        }
    }
}
