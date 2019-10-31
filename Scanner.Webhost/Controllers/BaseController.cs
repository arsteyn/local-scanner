using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using Bars.EAS.Utils.Extension;
using BM.DTO;
using Newtonsoft.Json;

namespace Scanner.Webhost.Controllers
{
    public class HomeController : ApiController
    {
        public HttpResponseMessage Get(string bookmakerName)
        {
            var module = ScannerApi.BookmakerScanners.FirstOrDefault(m => m.Name.ContainsIgnoreCase(bookmakerName));

            return module != null ? GetResponse(module.GetLines()) : new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        #region Private

        private static HttpResponseMessage GetResponse(LineDTO[] lines)
        {
            var json = JsonConvert.SerializeObject(lines);
            //var compressed = Lz4.CompressString(json);

            return new HttpResponseMessage
            {
                Content = new StringContent(json, Encoding.UTF8, "text/html")
            };
        }

        #endregion

    }
}
