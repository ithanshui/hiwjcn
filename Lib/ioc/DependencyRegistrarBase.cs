﻿using Autofac;
using Autofac.Extras.DynamicProxy;
using Autofac.Integration.Mvc;
using Lib.data;
using Lib.events;
using Lib.extension;
using Lib.helper;
using Lib.infrastructure;
using Lib.mvc.plugin;
using Lib.task;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lib.ioc
{
    /// <summary>
    /// 注册IOC依赖
    /// </summary>
    public abstract class DependencyRegistrarBase : IDependencyRegistrar
    {
        public abstract void Register(ref ContainerBuilder builder);

        /// <summary>
        /// 注册任务
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="ass"></param>
        protected void RegTasks(ref ContainerBuilder builder, params Assembly[] ass)
        {
            foreach (var a in ass)
            {
                var jobTypes = a.GetTypes().ToArray();
                jobTypes = jobTypes.Where(x => x.IsAssignableTo_<QuartzJobBase>() && x.IsNormalClass()).ToArray();
                if (ValidateHelper.IsPlumpList(jobTypes))
                {
                    builder.RegisterTypes(jobTypes).As<QuartzJobBase>();
                }
            }
        }

        /// <summary>
        /// 注册仓库
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="ass"></param>
        protected void RegDataRepository(ref ContainerBuilder builder, params Assembly[] ass)
        {
            foreach (var a in ass)
            {
                foreach (var t in a.GetTypes())
                {
                    if (t.BaseType != null && t.BaseType.IsGenericType_(typeof(EFRepository<>)))
                    {
                        builder.RegisterType(t).As(t);
                        builder.RegisterType(t).As(t.BaseType);
                        var interfaces = t.BaseType.GetInterfaces().Where(x => x.IsGenericType_(typeof(IRepository<>)));
                        if (interfaces?.Count() > 0)
                        {
                            builder.RegisterType(t).As(interfaces.ToArray());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="intercept"></param>
        /// <param name="ass"></param>
        protected void RegService(ref ContainerBuilder builder, bool intercept, params Assembly[] ass)
        {
            foreach (var a in ass)
            {
                foreach (var t in a.GetTypes())
                {
                    //注册service
                    if (t.BaseType != null && t.BaseType.IsGenericType_(typeof(ServiceBase<>)))
                    {
                        //实现iservicebase的接口
                        var interfaces = t.GetInterfaces().Where(x => x.GetInterfaces().Any(i => i.IsGenericType_(typeof(IServiceBase<>))));
                        if (intercept)
                        {
                            builder.RegisterType(t).As(t).EnableClassInterceptors();
                            builder.RegisterType(t).As(t.BaseType).EnableClassInterceptors();
                            if (interfaces?.Count() > 0)
                            {
                                builder.RegisterType(t).As(interfaces.ToArray()).EnableClassInterceptors();
                            }
                        }
                        else
                        {
                            builder.RegisterType(t).As(t);
                            builder.RegisterType(t).As(t.BaseType);
                            if (interfaces?.Count() > 0)
                            {
                                builder.RegisterType(t).As(interfaces.ToArray());
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 注册事件
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="ass"></param>
        protected void RegEvent(ref ContainerBuilder builder, params Assembly[] ass)
        {
            foreach (var a in ass)
            {
                try
                {
                    //找到包含consumer的类
                    var types = a.GetTypes().Where(x => x.GetInterfaces().Any(i => i.IsGenericType_(typeof(IConsumer<>))));
                    foreach (var t in types)
                    {
                        //找到接口
                        var interfaces = t.GetInterfaces().Where(x => x.IsGenericType_(typeof(IConsumer<>))).ToArray();
                        //注册到所有接口
                        builder.RegisterType(t).As(interfaces);
                    }
                }
                catch (Exception e)
                {
                    //Entity Framework 6不允许get types，抛了一个异常
                    e.SaveLog("注册事件发布异常");
                    continue;
                }
            }
            builder.RegisterType<EventPublisher>().As<IEventPublisher>().SingleInstance();
        }

        /// <summary>
        /// 获取插件程序集(还需完善)
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        protected List<Assembly> FindPluginAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.StartsWith("Hiwjcn.Plugin.")).ToList();
        }

        /// <summary>
        /// 注册插件的控制器
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="ass"></param>
        protected void RegController(ref ContainerBuilder builder, params Assembly[] ass)
        {
            foreach (var a in ass)
            {
                //注册URL
                builder.RegisterControllers(a);
                //注册插件
                var tps = a.GetTypes().Where(x => x.IsAssignableTo_<BasePaymentController>() && x.IsNormalClass()).ToArray();
                if (ValidateHelper.IsPlumpList(tps))
                {
                    builder.RegisterTypes(tps).As<BasePaymentController>();
                }
            }
        }

    }
}
