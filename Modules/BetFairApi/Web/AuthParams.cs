namespace BetFairApi.Web
{
    using System.Security.Cryptography.X509Certificates;
    
    public class AuthParams
    {
        public string Login { get; set; }

        public string Password { get; set; }

        public bool UseCertificate { get; set; }

        public X509Certificate2 Certificate { get; set; }
    }
}
