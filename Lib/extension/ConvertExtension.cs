﻿using System;
using System.Collections.Generic;
using System.Linq;
using Lib.helper;
using Lib.core;
using System.Collections.Specialized;

namespace Lib.extension
{
    public static class ConvertExtension
    {
        /// <summary>
        /// 转换为整型
        /// </summary>
        public static int ToInt(this string data, int? deft = default(int))
        {
            return ConvertHelper.GetInt(data, deft);
        }

        /// <summary>
        /// 转为float
        /// </summary>
        public static float ToFloat(this string data, float? deft = default(float))
        {
            return ConvertHelper.GetFloat(data, deft);
        }

        /// <summary>
        /// 转换为双精度浮点数,并按指定的小数位4舍5入
        /// </summary>
        public static double ToDouble(this string data, int? digits = null, double? deft = default(double))
        {
            var db = ConvertHelper.GetDouble(data, deft);
            if (digits != null)
            {
                return Math.Round(db, digits.Value);
            }
            return db;
        }

        /// <summary>
        /// 转换为高精度浮点数,并按指定的小数位4舍5入
        /// </summary>
        public static decimal ToDecimal(this string data, int? digits = null, decimal? deft = default(decimal))
        {
            var dec = ConvertHelper.GetDecimal(data, deft);
            if (digits != null)
            {
                return Math.Round(dec, digits.Value);
            }
            return dec;
        }

        /// <summary>
        /// 转换为日期
        /// </summary>
        public static DateTime ToDateTime(this string data, DateTime? deft)
        {
            return ConvertHelper.GetDateTime(data, deft);
        }

        /// <summary>
        /// 转为日期，默认值为当前时间
        /// </summary>
        public static DateTime ToDateTime(this string data)
        {
            return data.ToDateTime(DateTime.Now);
        }

        /// <summary>
        /// 转换为布尔值
        /// </summary>
        /// <param name="data">数据</param>
        public static bool ToBool(this string data)
        {
            var list = new string[] { "1", "true", "yes", "on", "success", "t", true.ToString() };
            return list.Contains(data.ToString().ToLower().Trim());
        }

        /// <summary>
        /// true为1，false为0
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int ToBoolInt(this string data)
        {
            return data.ToBool() ? 1 : 0;
        }

        /// <summary>
        /// 转为json 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ToJson(this object data)
        {
            return JsonHelper.ObjectToJson(data);
        }

        /// <summary>
        /// json转为实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T JsonToEntity<T>(this string json)
        {
            return JsonHelper.JsonToEntity<T>(json);
        }

        /// <summary>
        /// 映射
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T MapTo<T>(this object data)
        {
            return MapperHelper.GetMappedEntity<T>(data);
        }

        /// <summary>
        /// 映射
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="entity"></param>
        /// <param name="notmap"></param>
        public static void MapTo<T>(this object data, ref T entity, string[] notmap = null)
        {
            MapperHelper.MapEntity(ref entity, data, notmap);
        }

        /// <summary>
        /// 格式化数字，获取xxx xxxk xxxw
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string SimpleNumber(this Int64 num)
        {
            return Com.SimpleNumber(num);
        }

    }
}
