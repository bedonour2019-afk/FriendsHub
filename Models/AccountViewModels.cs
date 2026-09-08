using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FriendsHub.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "اكتب اسم المستخدم")]
        [Display(Name = "اسم المستخدم")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "اكتب الباسورد")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "اكتب اسم المستخدم")]
        [MinLength(3, ErrorMessage = "لازم يكون 3 حروف على الأقل")]
        [Display(Name = "اسم المستخدم")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "اكتب البريد الإلكتروني (جيميل)")]
        [EmailAddress(ErrorMessage = "اكتب بريد إلكتروني صحيح")]
        [Display(Name = "البريد الإلكتروني (Gmail)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "اكتب الباسورد")]
        [MinLength(4, ErrorMessage = "لازم يكون 4 حروف على الأقل")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "أكد الباسورد")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "الباسورد مش متطابق")]
        [Display(Name = "تأكيد كلمة المرور")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "اكتب كلمة السر الخاصة")]
        [Display(Name = "كلمة السر الخاصة")]
        public string SecretCode { get; set; } = string.Empty;

        public IFormFile? ProfilePicture { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "اكتب البريد الإلكتروني")]
        [EmailAddress(ErrorMessage = "اكتب بريد إلكتروني صحيح")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? GeneratedResetCode { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "اكتب البريد الإلكتروني")]
        [EmailAddress(ErrorMessage = "اكتب بريد إلكتروني صحيح")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "اكتب رمز الأمان / التحقق")]
        [Display(Name = "رمز التحقق (Code)")]
        public string ResetCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "اكتب كلمة المرور الجديدة")]
        [MinLength(4, ErrorMessage = "لازم تكون 4 حروف على الأقل")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور الجديدة")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "أكد كلمة المرور الجديدة")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "الباسورد مش متطابق")]
        [Display(Name = "تأكيد كلمة المرور")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
    }

    public class SettingsViewModel
    {
        [Required(ErrorMessage = "اكتب اسم المستخدم")]
        [MinLength(3, ErrorMessage = "لازم يكون 3 حروف على الأقل")]
        [Display(Name = "اسم المستخدم")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "اكتب البريد الإلكتروني")]
        [EmailAddress(ErrorMessage = "اكتب بريد إلكتروني صحيح")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        public string? CurrentPassword { get; set; }

        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "الباسورد مش متطابق")]
        [Display(Name = "تأكيد كلمة المرور الجديدة")]
        public string? ConfirmNewPassword { get; set; }

        public IFormFile? ProfilePicture { get; set; }

        public string? CurrentProfilePicture { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
