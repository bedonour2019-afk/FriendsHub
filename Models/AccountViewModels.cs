using System.ComponentModel.DataAnnotations;

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

        [Required(ErrorMessage = "اكتب كود المجموعة السرّي")]
        [DataType(DataType.Password)]
        [Display(Name = "كود المجموعة")]
        public string GroupPasscode { get; set; } = string.Empty;
    }
}