﻿using Lib.helper;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Configuration;

namespace Lib.extension
{
    public static class LogExtension
    {
        /// <summary>
        /// 获取深层的异常信息
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string GetInnerExceptionAsJson(this Exception e)
        {
            return Com.GetExceptionMsgJson(e);
        }

        /// <summary>
        /// 获取深层的异常信息
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static List<string> GetInnerExceptionAsList(this Exception e)
        {
            return Com.GetExceptionMsgList(e);
        }

        /// <summary>
        /// 使用log4net添加日志
        /// </summary>
        /// <param name="e"></param>
        /// <param name="name"></param>
        /// <param name="prefix"></param>
        public static void AddLog(this Exception e, string name = nameof(AddLog))
        {
            e.GetInnerExceptionAsJson().SaveErrorLog(name);
        }
        /// <summary>
        /// 使用log4net添加日志
        /// </summary>
        /// <param name="e"></param>
        /// <param name="t"></param>
        /// <param name="prefix"></param>
        public static void AddLog(this Exception e, Type t)
        {
            e.GetInnerExceptionAsJson().SaveErrorLog(t);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="log"></param>
        /// <param name="name"></param>
        public static void SaveErrorLog(this string log, string name = nameof(SaveErrorLog))
        {
            LogHelper.Error(name, log);
        }
        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="log"></param>
        /// <param name="t"></param>
        public static void SaveErrorLog(this string log, Type t)
        {
            LogHelper.Error(t, log);
        }

        /// <summary>
        /// 保存信息日志
        /// </summary>
        /// <param name="log"></param>
        /// <param name="name"></param>
        public static void SaveInfoLog(this string log, string name = nameof(SaveInfoLog))
        {
            LogHelper.Info(name, log);
        }
        /// <summary>
        /// 保存信息日志
        /// </summary>
        /// <param name="log"></param>
        /// <param name="t"></param>
        public static void SaveInfoLog(this string log, Type t)
        {
            LogHelper.Info(t, log);
        }

        /// <summary>
        /// 保存警告日志
        /// </summary>
        /// <param name="log"></param>
        /// <param name="name"></param>
        public static void SaveWarnLog(this string log, string name = nameof(SaveWarnLog))
        {
            LogHelper.Warn(name, log);
        }
        /// <summary>
        /// 保存警告日志
        /// </summary>
        /// <param name="log"></param>
        /// <param name="t"></param>
        public static void SaveWarnLog(this string log, Type t)
        {
            LogHelper.Info(t, log);
        }
    }

    public static class CommonLogExtension
    {
        public static readonly string LoggerName = ConfigurationManager.AppSettings["LoggerName"] ?? "WebLogger";

        /// <summary>
        /// 错误日志
        /// </summary>
        /// <param name="e"></param>
        /// <param name="prefix"></param>
        public static void AddErrorLog(this Exception e, string extra_data = null)
        {
            new
            {
                error_msg = e.GetInnerExceptionAsList(),
                req_data = ReqData(),
                extra_data = extra_data
            }.ToJson().SaveErrorLog(LoggerName);
        }

        /// <summary>
        /// 业务日志
        /// </summary>
        /// <param name="log"></param>
        public static void AddBusinessLog(this string log)
        {
            new
            {
                msg = log,
                req_data = ReqData()
            }.ToJson().SaveInfoLog(LoggerName);
        }

        /// <summary>
        /// 请求上下文信息 
        /// </summary>
        /// <returns></returns>
        private static object ReqData()
        {
            try
            {
                var url = HttpContext.Current.Request.Url.ToString();
                var p = HttpContext.Current.Request.Form.ToDict().ToUrlParam();
                return new { url = url, req_param = p };
            }
            catch (Exception e)
            {
                return new { msg = $"获取请求信息失败", error_list = e.GetInnerExceptionAsList() };
            }
        }
    }
}
