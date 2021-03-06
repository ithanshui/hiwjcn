﻿using Hiwjcn.Core.Infrastructure.Page;
using Lib.data;
using Lib.helper;
using Lib.infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLogic.Dal.Page;
using WebLogic.Model.Page;
using Lib.extension;

namespace WebLogic.Bll.Page
{
    /// <summary>
    /// 内容块
    /// </summary>
    public class PageService : ServiceBase<SectionModel>, IPageService
    {
        private SectionDal _SectionDal { get; set; }
        private IRepository<SectionModel> _PageRepository { get; set; }

        public PageService(IRepository<SectionModel> page)
        {
            this._SectionDal = new SectionDal();
            this._PageRepository = page;
        }

        public override string CheckModel(SectionModel model)
        {
            if (model == null)
            {
                return "对象为空";
            }
            if (!ValidateHelper.IsPlumpString(model.SectionName))
            {
                return "内容块名称不能为空";
            }
            if (!ValidateHelper.IsPlumpString(model.SectionTitle))
            {
                return "标题不能为空";
            }
            if (!ValidateHelper.IsPlumpString(model.SectionDescription))
            {
                return "描述不能为空";
            }
            if (!ValidateHelper.IsPlumpString(model.SectionContent))
            {
                return "内容不能为空";
            }
            if (!ValidateHelper.IsPlumpString(model.SectionType))
            {
                return "内容类型不能为空";
            }
            return string.Empty;
        }

        /// <summary>
        /// 获取内容块
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public SectionModel GetSection(string name, string section_type = null)
        {
            var list = GetSections(new string[] { name }, section_type);
            if (ValidateHelper.IsPlumpList(list))
            {
                return list[0];
            }
            return null;
        }

        /// <summary>
        /// 获取多个内容块
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public List<SectionModel> GetSections(string[] names = null, string section_type = null, string rel_group = null, int maxcount = 100)
        {
            if (names == null) { names = new string[] { }; }
            string key = GetCacheKey($"{nameof(PageService)}.{nameof(GetSections)}",
                string.Join("|", names), section_type, rel_group, maxcount.ToString());
            return Cache(key, () =>
            {
                List<SectionModel> data = null;

                _SectionDal.PrepareIQueryable((query) =>
                {
                    if (ValidateHelper.IsPlumpList(names))
                    {
                        query = query.Where(x => names.Contains(x.SectionName));
                    }
                    if (ValidateHelper.IsPlumpString(section_type))
                    {
                        query = query.Where(x => x.SectionType == section_type);
                    }
                    if (ValidateHelper.IsPlumpString(rel_group))
                    {
                        query = query.Where(x => x.RelGroup == rel_group);
                    }
                    query = query.OrderByDescending(x => x.UpdateTime).Skip(0).Take(maxcount);
                    data = query.ToList();
                    return true;
                });

                return data;
            });
        }

        /// <summary>
        /// 添加内容快
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public string AddSection(SectionModel model)
        {
            string errinfo = CheckModel(model);
            if (ValidateHelper.IsPlumpString(errinfo)) { return errinfo; }
            if (_SectionDal.Exist(x => x.SectionName == model.SectionName))
            {
                return "标识存在重复值";
            }
            return _SectionDal.Add(model) > 0 ? SUCCESS : "添加失败";
        }

        /// <summary>
        /// 删除并添加
        /// </summary>
        /// <param name="updatemodel"></param>
        /// <returns></returns>
        public string UpdateSection(SectionModel updatemodel)
        {
            var model = _SectionDal.GetFirst(x => x.SectionID == updatemodel.SectionID);
            if (model == null) { return "内容不存在"; }

            model.SectionName = updatemodel.SectionName;
            model.SectionTitle = updatemodel.SectionTitle;
            model.SectionDescription = updatemodel.SectionDescription;
            model.SectionContent = updatemodel.SectionContent;
            model.SectionType = updatemodel.SectionType;
            model.RelGroup = updatemodel.RelGroup;
            model.UpdateTime = DateTime.Now;

            string errinfo = CheckModel(model);
            if (ValidateHelper.IsPlumpString(errinfo)) { return errinfo; }

            if (_SectionDal.Exist(x =>
                x.SectionName == model.SectionName &&
                x.SectionID != model.SectionID))
            {
                return "标识存在重复值";
            }

            return _SectionDal.Update(model) > 0 ? SUCCESS : "更新失败";
        }

        /// <summary>
        /// 删除section
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public string DeleteSections(params string[] names)
        {
            names = ConvertHelper.NotNullList(names).Where(x => ValidateHelper.IsPlumpString(x)).ToArray();
            if (!ValidateHelper.IsPlumpList(names)) { return "没有指定删除对象"; }

            var data = _SectionDal.GetList(x => names.Contains(x.SectionName));
            if (!ValidateHelper.IsPlumpList(data)) { return "您要删除的数据不存在"; }
            int count = _SectionDal.Delete(data.ToArray());
            return count > 0 ? SUCCESS : "删除失败";
        }

        /// <summary>
        /// 获取section列表
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pagesize"></param>
        /// <param name="loadCount"></param>
        /// <returns></returns>
        public PagerData<SectionModel> GetSectionList(
            string q = null, string sectionType = null,
            int page = 1, int pagesize = 10)
        {
            string key = GetCacheKey($"{nameof(PageService)}.{nameof(GetSectionList)}", q, sectionType, page.ToString(), pagesize.ToString());

            return Cache(key, () =>
            {
                var data = new PagerData<SectionModel>();
                //列表
                _SectionDal.PrepareIQueryable((query) =>
                {
                    if (ValidateHelper.IsPlumpString(q))
                    {
                        query = query.Where(x => x.SectionTitle.Contains(q) || x.SectionDescription.Contains(q));
                    }
                    if (ValidateHelper.IsPlumpString(sectionType))
                    {
                        query = query.Where(x => x.SectionType == sectionType);
                    }
                    data.ItemCount = query.Count();
                    data.DataList = query.OrderByDescending(x => x.UpdateTime).QueryPage(page, pagesize).ToList();
                    return true;
                });
                return data;
            });
        }

    }
}
