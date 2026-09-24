namespace MvcApp.Common.Models.Captcha
{
    public class Registration
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string CaptchaToken { get; set; }

        public Registration(string username, string password, string email, string captchaToken)
        {
            Username = username;
            Password = password;
            Email = email;
            CaptchaToken = captchaToken;
        }
    }
}
