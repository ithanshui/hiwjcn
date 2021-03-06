﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib.mvc.attr;
using Lib.extension;
using Lib.helper;
using System.Web.Mvc;
using System.Web;
using Lib.ioc;
using Lib.mvc;

namespace Lib.mvc.user
{
    /// <summary>
    /// 验证登录权限
    /// </summary>
    public abstract class ValidLoginBaseAttribute : _ActionFilterBaseAttribute
    {
        /// <summary>
        /// 权限，逗号隔开
        /// </summary>
        public string Permission { get; set; }

        /// <summary>
        /// 没有登录的时候调用
        /// </summary>
        /// <param name="filterContext"></param>
        public abstract void WhenNotLogin(ref ActionExecutingContext filterContext);

        /// <summary>
        /// 没有权限的时候调用
        /// </summary>
        /// <param name="filterContext"></param>
        public abstract void WhenNoPermission(ref ActionExecutingContext filterContext);

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var context = HttpContext.Current;
            var _loginstatus = this.GetLoginStatus();

            var loginuser = _loginstatus.GetLoginUser(context);

            if (loginuser == null)
            {
                this.WhenNotLogin(ref filterContext);
                return;
            }

            if (ValidateHelper.IsPlumpString(this.Permission))
            {
                if (this.Permission.Split(',').Where(x => x?.Length > 0).Any(x => !loginuser.HasPermission(x)))
                {
                    this.WhenNoPermission(ref filterContext);
                    return;
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }

    public class 页面权限拦截Attribute : ValidLoginBaseAttribute
    {
        public override LoginStatus GetLoginStatus() => AppContext.GetObject<LoginStatus>();

        public override void WhenNoPermission(ref ActionExecutingContext filterContext)
        {
            filterContext.Result = new ViewResult() { ViewName = "~/Views/Shared/Limited.cshtml" };
        }

        public override void WhenNotLogin(ref ActionExecutingContext filterContext)
        {
            var current_url = filterContext.HttpContext.Request.Url.ToString();
            current_url = EncodingHelper.UrlEncode(current_url);
            filterContext.Result = new RedirectResult($"/account/login?continue={current_url}");
        }
    }

    public class 接口权限拦截Attribute : ValidLoginBaseAttribute
    {
        public override LoginStatus GetLoginStatus() => AppContext.GetObject<LoginStatus>();

        public override void WhenNoPermission(ref ActionExecutingContext filterContext)
        {
            filterContext.Result = GetJson(new _() { success = false, msg = "没有权限" });
        }

        public override void WhenNotLogin(ref ActionExecutingContext filterContext)
        {
            filterContext.Result = GetJson(new _() { success = false, msg = "登录过期，请刷新页面" });
        }
    }
}
