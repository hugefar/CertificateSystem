using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using CertificateSystem.Web.Identity;

namespace CertificateSystem.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            var model = new CertificateSystem.Web.Models.LoginViewModel { ReturnUrl = returnUrl };
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(CertificateSystem.Web.Models.LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 验证验证码
            var sessionCaptcha = HttpContext.Session.GetString("CaptchaCode");
            if (string.IsNullOrEmpty(sessionCaptcha) || string.IsNullOrEmpty(model.Captcha) || !string.Equals(sessionCaptcha, model.Captcha, System.StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "验证码错误。请重新输入。");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(string.Empty, "用户名和密码均为必填项。");
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "账户不存在或已被禁用。");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, isPersistent: model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                user.LastLoginTime = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "账号已被锁定，请稍后再试。");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "用户名或密码错误。");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [AllowAnonymous]
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Captcha()
        {
            // 生成 4 位验证码，排除易混淆字符
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // 去掉 I, O, 0,1
            var rand = new Random();
            var codeChars = new char[4];
            for (int i = 0; i < 4; i++)
                codeChars[i] = chars[rand.Next(chars.Length)];
            var code = new string(codeChars);

            HttpContext.Session.SetString("CaptchaCode", code);

            const int width = 120;
            const int height = 40;
            using var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            // 画干扰线
            using (var pen = new Pen(Color.LightGray, 1))
            {
                for (int i = 0; i < 8; i++)
                {
                    var x1 = rand.Next(width);
                    var y1 = rand.Next(height);
                    var x2 = rand.Next(width);
                    var y2 = rand.Next(height);
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
            }

            // 绘制字符，随机颜色、位置和角度
            var fonts = new[] { "Arial", "Tahoma", "Verdana" };
            for (int i = 0; i < code.Length; i++)
            {
                var ch = code[i].ToString();
                var fontName = fonts[rand.Next(fonts.Length)];
                using var font = new Font(fontName, 20, FontStyle.Bold);
                var brush = new SolidBrush(Color.FromArgb(rand.Next(50, 150), rand.Next(50, 150), rand.Next(50, 150)));
                var x = 10 + i * 24 + rand.Next(-2, 3);
                var y = rand.Next(2, 10);
                var state = g.Save();
                var angle = rand.Next(-25, 25);
                g.TranslateTransform(x, y);
                g.RotateTransform(angle);
                g.DrawString(ch, font, brush, 0, 0);
                g.Restore(state);
                brush.Dispose();
            }

            // 添加随机噪点
            for (int i = 0; i < 80; i++)
            {
                var x = rand.Next(width);
                var y = rand.Next(height);
                bmp.SetPixel(x, y, Color.FromArgb(rand.Next(100, 256), rand.Next(100, 256), rand.Next(100, 256)));
            }

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return File(ms.ToArray(), "image/png");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
    }
}
