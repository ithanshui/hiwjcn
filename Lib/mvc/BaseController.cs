﻿using Lib.core;
using Lib.extension;
using Lib.helper;
using Lib.ioc;
using Lib.mvc.user;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Configuration;

namespace Lib.mvc
{
    //[OutputCache(Duration = 10)]
    [ValidateInput(false)]
    public abstract class BaseController : System.Web.Mvc.Controller
    {
        /// <summary>
        /// 访问上下文
        /// </summary>
        public WebWorkContext X { get; private set; }

        public BaseController()
        {
            X = new WebWorkContext();
        }

        [NonAction]
        protected int? CheckPage(int? page) => Com.CheckPage(page);

        [NonAction]
        protected int? CheckPageSize(int? size) => Com.CheckPageSize(size);

        #region 返回结果
        /// <summary>
        /// 重写json方法，解决mvc中json丢时区的问题
        /// </summary>
        /// <param name="data"></param>
        /// <param name="contentType"></param>
        /// <param name="contentEncoding"></param>
        /// <returns></returns>
        [NonAction]
        protected override JsonResult Json(object data, string contentType, Encoding contentEncoding)
        {
            return this.Json(data, contentType, contentEncoding, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 重写json方法，解决mvc中json丢时区的问题
        /// </summary>
        /// <param name="data"></param>
        /// <param name="contentType"></param>
        /// <param name="contentEncoding"></param>
        /// <param name="behavior"></param>
        /// <returns></returns>
        [NonAction]
        protected override JsonResult Json(object data, string contentType, Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new CustomJsonResult()
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = behavior
            };
        }

        /// <summary>
        /// 获取json
        /// </summary>
        [NonAction]
        public ActionResult GetJson(object obj, JsonRequestBehavior behavior = JsonRequestBehavior.AllowGet)
        {
            return Json(obj, behavior);
        }

        /// <summary>
        /// 获取jsonp
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult GetJsonp(object obj, string callback = "callback")
        {
            var func = this.Request.QueryString[callback];
            return Content($"{func}({obj.ToJson()})", "text/javascript");
        }

        /// <summary>
        /// 判断是否成功
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [NonAction]
        public bool IsSuccess(string msg) => !ValidateHelper.IsPlumpString(msg);

        /// <summary>
        /// 获取默认的json
        /// </summary>
        /// <param name="errmsg"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult GetJsonRes(string errmsg)
        {
            return GetJson(new _() { success = IsSuccess(errmsg), msg = errmsg });
        }

        /// <summary>
        /// 返回json
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult StringAsJson(string json)
        {
            return Content(json, "text/json");
        }

        /// <summary>
        /// 系统错误
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public ActionResult Http500()
        {
            return new Http500();
        }

        /// <summary>
        /// 找不到页面
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public ActionResult Http404()
        {
            return new Http404();
        }

        /// <summary>
        /// 永久跳转
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult Http301(string url)
        {
            return new Http301(url);
        }

        /// <summary>
        /// 没有权限
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public ActionResult Http403()
        {
            return new Http403();
        }

        /// <summary>
        /// 去首页
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public ActionResult GoHome()
        {
            return new GoHomeResult();
        }
        #endregion

        #region action处理

        protected ActionResult ErrorResult { get; set; }
        protected ActionResult NoLoginResult { get; set; }
        protected ActionResult NoPermissionResult { get; set; }
        protected List<string> PermissionList { get; set; }
        protected readonly bool ShowExceptionResult = (ConfigurationManager.AppSettings["ShowExceptionResult"] ?? "true").ToBool();

        [NonAction]
        protected virtual ActionResult ExceptionResult(Exception e)
        {
            var (area, controller, action) = this.RouteData.GetA_C_A();
            e.AddLog(this.GetType());
            //自定义错误
            if (this.ErrorResult != null)
            {
                return this.ErrorResult;
            }
            //捕获的错误
            if (this.ShowExceptionResult)
            {
                return GetJson(e.GetInnerExceptionAsList());
            }
            //默认500页面
            return Http500();
        }

        /// <summary>
        /// 获取action的时候捕获异常
        /// </summary>
        /// <param name="GetActionFunc"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult RunAction(Func<ActionResult> GetActionFunc)
        {
            try
            {
                return GetActionFunc.Invoke();
            }
            catch (Exception e)
            {
                return ExceptionResult(e);
            }
        }

        /// <summary>
        /// 异步，还没有使用
        /// </summary>
        /// <param name="GetActionFunc"></param>
        /// <returns></returns>
        [NonAction]
        public async Task<ActionResult> RunActionAsync(Func<Task<ActionResult>> GetActionFunc)
        {
            try
            {
                return await GetActionFunc.Invoke();
            }
            catch (Exception e)
            {
                return ExceptionResult(e);
            }
        }

        /// <summary>
        /// 通过session验证身份
        /// 里面不要捕获异常，此方法会自动记录日志
        /// loginuser为有效登陆用户，用户ID>0
        /// </summary>
        /// <param name="GetActionFunc"></param>
        /// <returns></returns>
        [NonAction]
        public ActionResult RunActionWhenLogin(Func<LoginUserInfo, ActionResult> GetActionFunc)
        {
            return RunAction(() =>
            {
                var (loginuser, error) = TryGetLoginUser();
                if (error != null)
                {
                    return error;
                }

                return GetActionFunc.Invoke(loginuser);
            });
        }

        /// <summary>
        /// 异步实现
        /// </summary>
        /// <param name="GetActionFunc"></param>
        /// <returns></returns>
        [NonAction]
        public async Task<ActionResult> RunActionWhenLoginAsync(Func<LoginUserInfo, Task<ActionResult>> GetActionFunc)
        {
            return await RunActionAsync(async () =>
            {
                var (loginuser, error) = TryGetLoginUser();
                if (error != null)
                {
                    return error;
                }

                return await GetActionFunc.Invoke(loginuser);
            });
        }

        /// <summary>
        /// 没有登录的时候使用这个返回，可以重写
        /// </summary>
        /// <returns></returns>
        protected virtual ActionResult WhenNoLogin()
        {
            if (NoLoginResult != null)
            {
                return NoLoginResult;
            }
            else
            {
                //没有登陆就跳转登陆
                var redirect_url = AppContext.GetObject<IGetLoginUrl>().GetUrl(this.X.Url);
                return new RedirectResult(redirect_url);
            }
        }

        /// <summary>
        /// 没有权限的时候调用，可以重写
        /// </summary>
        /// <returns></returns>
        protected virtual ActionResult WhenNoPermission()
        {
            if (NoPermissionResult != null)
            {
                return NoPermissionResult;
            }
            else
            {
                return Http403();
            }
        }

        protected virtual LoginUserInfo TryGetUser()
        {
            var loginuser = this.X.User;
            if (loginuser == null)
            {
                //尝试通过token获取登陆用户
                var token = this.Request.Headers["token"];
                if (ValidateHelper.IsPlumpString(token))
                {
                    //loginuser = null;
                }
            }
            return loginuser;
        }

        /// <summary>
        /// 验证是否有权限
        /// </summary>
        /// <param name="loginuser"></param>
        /// <returns></returns>
        protected virtual bool HasPermission(LoginUserInfo loginuser)
        {
            return ConvertHelper.NotNullList(PermissionList).Any(x => !loginuser.HasPermission(x));
        }

        [NonAction]
        private (LoginUserInfo loginuser, ActionResult res) TryGetLoginUser()
        {
            var loginuser = TryGetUser();
            //==================================================================================================
            //如果没登陆就跳转
            if (loginuser == null)
            {
                return (loginuser, WhenNoLogin());
            }
            //所需要的全部权限值
            if (HasPermission(loginuser))
            {
                return (loginuser, WhenNoPermission());
            }
            return (loginuser, null);
        }

        #endregion

    }
}
