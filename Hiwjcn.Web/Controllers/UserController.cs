﻿using Bll.Sys;
using Hiwjcn.Core.Infrastructure.User;
using Hiwjcn.Web.Models.User;
using Lib.helper;

using Lib.io;
using Lib.mvc;
using Lib.mvc.user;
using Model.Sys;
using Model.User;
using System;
using System.Web.Mvc;

namespace Hiwjcn.Web.Controllers
{
    public class UserController : WebCore.MvcLib.Controller.UserBaseController
    {
        private IUserService _IUserService { get; set; }
        private readonly LoginStatus _LoginStatus;
        public UserController(IUserService user, LoginStatus _LoginStatus)
        {
            this._IUserService = user;
            this._LoginStatus = _LoginStatus;
        }


        public ActionResult NoTest()
        {
            this._LoginStatus.SetUserLogin(loginuser: new LoginUserInfo()
            {
                IID = 99,
                UserID = Com.GetUUID(),
                UserUID = Com.GetUUID(),
                LoginToken = Com.GetUUID(),
                Email = "hiwjcn@live.com",
                IsActive = 1
            });
            return Content("");
        }

        [页面权限拦截(Permission = "不存在的权限")]
        public ActionResult No()
        {
            return Content("");
        }

        [接口权限拦截(Permission = "测试用的权限")]
        public ActionResult NoApi()
        {
            return GetJson(new _() { });
        }

        public ActionResult AllPers()
        {
            var pers = this.ScanAllAssignedPermissionOnThisAssembly();
            return GetJson(pers);
        }

        /// <summary>
        /// 用户信息
        /// </summary>
        /// <returns></returns>
        public ActionResult UserInfo()
        {
            return RunActionWhenLogin((loginuser) =>
            {
                var model = _IUserService.GetByID(loginuser.IID);
                if (model == null) { return Http404(); }
                ViewData["user"] = model;
                return View();
            });
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult InfoAction(string user_name, string user_sex, string user_phone,
            string user_qq, string user_introduction)
        {
            return RunActionWhenLogin((loginuser) =>
            {
                var model = new UserModel();
                model.UserID = loginuser.IID;
                model.NickName = user_name;
                model.Sex = user_sex;
                model.Phone = user_phone;
                model.QQ = user_qq;
                model.Introduction = user_introduction;

                var res = _IUserService.UpdateUserInfo(model);
                if (!ValidateHelper.IsPlumpString(res))
                {
                    //
                }
                return GetJsonRes(res);
            });
        }

        /// <summary>
        /// 个人信息展示页面
        /// </summary>
        /// <returns></returns>
        public ActionResult Me(int? id)
        {
            return RunAction(() =>
            {
                id = id ?? (this.X.LoginUser?.IID ?? 0);

                if (id <= 0)
                {
                    return GoHome();
                }

                var user = _IUserService.GetByID(id.Value);
                if (user == null) { return GoHome(); }

                var model = new MeViewModel();
                model.User = user;
                model.IsMe = model.User.UserID == (this.X.LoginUser?.IID ?? 0);

                ViewData["model"] = model;

                return View();
            });
        }

        /// <summary>
        /// 更新用户头像
        /// </summary>
        /// <returns></returns>
        public ActionResult UpdateUserMask()
        {
            return RunActionWhenLogin((loginuser) =>
            {
                var model = _IUserService.GetByID(loginuser.IID);
                ViewData["model"] = model;
                return View();
            });
        }

        [HttpPost]
        public ActionResult UpdateUserMaskAction()
        {
            return RunActionWhenLogin((loginuser) =>
            {
                var file = this.X.context.Request.Files["user_mask"];
                var save_path = this.X.context.Server.MapPath("~/static/upload/usermask/");
                var res = _IUserService.UpdateUserMask(loginuser.IID, file, save_path);
                if (!ValidateHelper.IsPlumpString(res))
                {
                    return Redirect("/user/updateusermask/");
                }
                return Content("请求未能完成：" + res);
            });
        }

        /// <summary>
        /// 用户头像
        /// </summary>
        /// <returns></returns>
        [Route("User/UserMask/{id}/")]
        public ActionResult UserMask(int? id)
        {
            return RunAction(() =>
            {
                id = id ?? 0;
                if (id <= 0) { return Content("!"); }

                var b = _IUserService.GetUserImage(id.Value);
                if (!ValidateHelper.IsPlumpList(b))
                {
                    string deftImage = Server.MapPath("~/static/image/moren.png");
                    b = IOHelper.GetFileBytes(deftImage);
                }
                if (!ValidateHelper.IsPlumpList(b))
                {
                    return Content("空数据");
                }
                ResponseHelper.SetResponseNoCache(this.X.context.Response);
                return File(b, "Image/Png");
            });
        }

        /// <summary>
        /// 站内信
        /// </summary>
        /// <returns></returns>
        public ActionResult Msg()
        {
            return RunActionWhenLogin((loginuser) =>
            {
                var bll = new MessageBll();

                ViewData["list"] = bll.GetTopMessage(loginuser.IID, 100);

                return View();
            });
        }

        public ActionResult MsgCount()
        {
            return Content("");
        }

        public ActionResult MsgShow()
        {
            return Content("");
        }

        /// <summary>
        /// 发送站内信处理请求
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SendMessageAction(int? to, string msg)
        {
            return RunActionWhenLogin((loginuser) =>
            {
                to = to ?? 0;

                var bll = new MessageBll();

                var start = DateTime.Now.Date;
                var end = start.AddDays(1);
                int count = bll.GetSenderMessageCount(loginuser.IID, start, end);
                if (count >= 30)
                {
                    return GetJsonRes("您今天发送条数达到上限");
                }

                var model = new MessageModel();
                model.MsgContent = msg;
                model.ReceiverUserID = to.Value;
                model.SenderUserID = loginuser.IID;
                model.UpdateTime = DateTime.Now;
                model.IsNew = "true";

                return GetJsonRes(bll.SendMessage(model));
            });
        }

        /// <summary>
        /// 发送站内信页面
        /// </summary>
        /// <returns></returns>
        public ActionResult SendMessage(int? to)
        {
            return RunActionWhenLogin((loginuser) =>
            {
                int toID = to ?? 0;
                if (toID <= 0 || toID == loginuser.IID) { return Content("参数错误"); }
                var model = _IUserService.GetByID(toID);
                if (model == null) { return Content("用户不存在"); }

                ViewData["model"] = model;

                return View();
            });
        }

    }
}
