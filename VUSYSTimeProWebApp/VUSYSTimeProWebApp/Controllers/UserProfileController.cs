using Microsoft.AspNetCore.Mvc;
using VUSYSTimeProWebApp.Contracts;
using VUSYSTimeProWebApp.DACHelper;
using VUSYSTimeProWebApp.Filters;
using VUSYSTimeProWebApp.Models;
using VUSYSTimeProWebApp.ViewModels;
using CommonLib;
using Newtonsoft.Json;

namespace VUSYSTimeProWebApp.Controllers
{
    [SessionTimeout]
    public class UserProfileController : Controller
    {
        private AppDbContext db = null;
        private readonly IUser IUserSvc = null;
        private readonly IWebHostEnvironment webHostEnvironment;

        /// <summary>
        /// User Profile Controller Class Constructor
        /// </summary>
        /// <param name="db"></param>
        /// <param name="ServiceRepository"></param>
        /// <param name="hostEnvironment"></param>
        public UserProfileController(AppDbContext db, IUser ServiceRepository, IWebHostEnvironment hostEnvironment)
        {
            this.db = db;
            this.IUserSvc = ServiceRepository;
            webHostEnvironment = hostEnvironment;
        }

        /// <summary>
        /// This Get Action Method is Used to Invoke User Profile Screen.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> UserProfile(AllViewModel allViewModel)
        {
            try
            {
                var SessionDetails = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");

                if (SessionDetails != null)
                {
                    int id = SessionDetails.Id;
                    var UserDetails = await IUserSvc.GetUserDetails(id);
                    allViewModel.Users = UserDetails;
                    ViewBag.RoleList = DropdownHelper.FillRoleList(db);
                    string strbase64 = string.Empty;
                    byte[] myimage = new byte[0];
                    var UserProfileImage = string.Empty;
                    if (UserDetails.UserImageFileName == "")
                    {
                        UserDetails.UserImageFileName = "Default_Male.png";
                    }
                    if (UserDetails.UserImage != null)
                    {
                        myimage = (byte[])UserDetails.UserImage;
                        strbase64 = Convert.ToBase64String(myimage);
                        UserProfileImage = "data:Image/jpg;base64," + strbase64;
                    }
                    else
                    {
                        string DefaultUserImageFileName = "Default_Male.png";
                        string webRootPath = webHostEnvironment.WebRootPath;
                        var fullPath = webRootPath + "\\Images\\" + DefaultUserImageFileName;
                        myimage = System.IO.File.ReadAllBytes(fullPath);
                        strbase64 = Convert.ToBase64String(myimage);
                        UserProfileImage = "data:Image/jpg;base64," + strbase64;
                    }
                    ViewBag.UserProfileImage = UserProfileImage;
                    ViewBag.UserName = UserDetails.UserName;
                    ViewBag.UserEmailID = UserDetails.UserEmailID;
                    if (allViewModel != null)
                    {
                        return View(allViewModel);
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "UserProfileController.UserProfile()", ex.Message, false);
                TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                return RedirectToAction("CommonError", "Error");
            }
            return View();
        }

        /// <summary>
        /// This Post Action Method is Used to Update User Profile Details.
        /// </summary>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserProfile(AllViewModel allViewModel)
        {
            if (allViewModel.Users.Password == null || allViewModel.Users.MobileNo == null)
            {
                ModelState.Clear();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    string Message = await UpdateUserDetails(allViewModel.Users.Id, allViewModel);
                    if (Message != string.Empty)
                    {
                        TempData[Helper.TEMP_DATA_SUCCESS_MESSAGE_KEY] = Message;
                    }
                }
                catch (Exception ex)
                {
                    Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "UserProfileController.UpdateUserProfile()", ex.Message, false);
                    TempData[Helper.TEMP_DATA_COMMON_ERROR_MESSAGE_KEY] = JsonConvert.SerializeObject(CommonHelper.GenerateCommonError("Error500.png", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), "Return To Previous Page", "An Error Occurred. Please Try After Sometime. Meanwhile, you may", true));
                    return RedirectToAction("CommonError", "Error");
                }
            }
            ModelState.Clear();
            return RedirectToAction("UserProfile");
        }

        #region [Custom Methods]

        /// <summary>
        /// This Method Is Used To Update User Details.
        /// </summary>
        /// <param name="Uid"></param>
        /// <param name="allViewModel"></param>
        /// <returns></returns>
        public async Task<string> UpdateUserDetails(int Uid, AllViewModel allViewModel)
        {
            string Message = string.Empty;
            try
            {
                allViewModel.Users.Uid = Uid;
                var Files = Request.Form.Files;
                byte[] _myData = null;
                string FileName = string.Empty;
                string webRootPath = string.Empty;

                if (Files.Count > 0)
                {
                    FileName = allViewModel.userProfileImage.ProfileImage.FileName;
                    string ImagePath = CommonHelper.GetActualPath(webHostEnvironment, "UploadedImages");

                    if (System.IO.File.Exists(ImagePath))
                    {
                        System.IO.File.Delete(ImagePath);
                    }

                    string filePath = Path.Combine(ImagePath, FileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await allViewModel.userProfileImage.ProfileImage.CopyToAsync(fileStream);
                    }

                    webRootPath = webHostEnvironment.WebRootPath;

                    var fullPath = ImagePath + "\\" + FileName;

                    _myData = CommonHelper.ConvertToBytes(allViewModel.userProfileImage.ProfileImage);

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                    var UserDetailSession = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                    if (UserDetailSession != null)
                    {
                        allViewModel.Users.MobileNo = UserDetailSession.MobileNo;
                        allViewModel.Users.RoleID = UserDetailSession.RoleID;
                        allViewModel.Users.UserName = UserDetailSession.UserName;
                        allViewModel.Users.IsDeleted = UserDetailSession.IsDeleted;
                    }
                }
                else
                {
                    if (allViewModel.Users.Uid == 0)
                    {
                        string DefaultUserImageFileName = "Default_Male.png";
                        webRootPath = webHostEnvironment.WebRootPath;
                        var fullPath = webRootPath + "\\Images\\" + DefaultUserImageFileName;
                        _myData = System.IO.File.ReadAllBytes(fullPath);
                        FileName = DefaultUserImageFileName;
                    }
                    else
                    {
                        var UserDetailSession = CommonHelper.GetObjectFromJson<UsersM>(HttpContext.Session, "UserSession");
                        if (UserDetailSession != null)
                        {
                            _myData = UserDetailSession.UserImage;
                            FileName = UserDetailSession.UserImageFileName;
                            allViewModel.Users.MobileNo = UserDetailSession.MobileNo;
                            allViewModel.Users.RoleID = UserDetailSession.RoleID;
                            allViewModel.Users.UserName = UserDetailSession.UserName;
                            allViewModel.Users.IsDeleted = UserDetailSession.IsDeleted;
                        }
                    }
                }
                allViewModel.Users.UserImageFileName = FileName;
                allViewModel.Users.UserImage = _myData;
                allViewModel.Users.Status = "ADD";
                Message = await IUserSvc.SaveUpdateUserDetails(db, webHostEnvironment, allViewModel.Users);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog,"VUSYSTimeProWebApp.Controllers", "UserProfileController.UpdateUserDetails()", ex.Message, false);
                throw;
            }
            return Message;
        }

        #endregion [Custom Methods]
    }
}
