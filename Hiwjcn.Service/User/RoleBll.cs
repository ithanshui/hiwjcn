﻿using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebLogic.Model.User;
using Bll;
using Lib.core;
using Lib.helper;
using WebLogic.Dal.User;
using Hiwjcn.Core.Infrastructure.User;
using Lib.infrastructure;

namespace WebLogic.Bll.User
{
    /// <summary>
    /// 角色
    /// </summary>
    public class RoleBll : ServiceBase<RoleModel>, IRoleService
    {
        private RoleDal _RoleDal { get; set; }
        private RolePermissionDal _RolePermissionDal { get; set; }

        public RoleBll()
        {
            this._RoleDal = new RoleDal();
            this._RolePermissionDal = new RolePermissionDal();
        }

        public override string CheckModel(RoleModel model)
        {
            if (model == null) { return "角色对象为空"; }
            if (!ValidateHelper.IsPlumpString(model.RoleName)) { return "角色名称为空"; }
            return string.Empty;
        }

        /// <summary>
        /// 添加角色
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public string AddRole(RoleModel model)
        {
            string errorinfo = CheckModel(model);
            if (ValidateHelper.IsPlumpString(errorinfo)) { return errorinfo; }
            return _RoleDal.Add(model) > 0 ? SUCCESS : "添加失败";
        }

        /// <summary>
        /// 跟新角色
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public string UpdateRole(RoleModel model)
        {
            if (model.RoleID <= 0) { return "错误的ID"; }
            var role = _RoleDal.GetFirst(x => x.RoleID == model.RoleID);
            if (role == null) { return "角色不存在"; }
            role.RoleName = model.RoleName;
            role.RoleDescription = model.RoleDescription;
            role.AutoAssignRole = model.AutoAssignRole;
            string errinfo = CheckModel(model);
            if (ValidateHelper.IsPlumpString(errinfo)) { return errinfo; }
            return _RoleDal.Update(role) > 0 ? SUCCESS : "更新失败";
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string DeleteRole(int RoleID)
        {
            if (RoleID <= 0) { return "错误的ID"; }
            var role = _RoleDal.GetFirst(x => x.RoleID == RoleID);
            if (role == null) { return "角色不存在"; }
            return _RoleDal.Delete(role) > 0 ? SUCCESS : "删除失败";
        }

        /// <summary>
        /// 获取角色
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public RoleModel GetByID(int id)
        {
            if (id <= 0) { return null; }
            string key = "rolebyid:" + id;
            return Cache(key, () =>
            {
                return _RoleDal.GetFirst(x => x.RoleID == id);
            });
        }

        /// <summary>
        /// 获取所有角色
        /// </summary>
        /// <returns></returns>
        public List<RoleModel> GetAllRoles()
        {
            string key = "allrolescachekey";
            return Cache(key, () =>
            {
                return _RoleDal.QueryList(where: null, orderby: x => x.RoleID);
            });
        }

        /// <summary>
        /// 获取角色下的所有权限id
        /// </summary>
        /// <param name="role_id"></param>
        /// <returns></returns>
        public List<string> GetRolePermissionsList(int role_id)
        {
            if (role_id <= 0) { return null; }
            string key = "rolespermission_by_id" + role_id;
            return Cache(key, () =>
            {
                var list = _RolePermissionDal.QueryList(where: x => x.RoleID == role_id, orderby: x => x.RolePermissionID);
                if (ValidateHelper.IsPlumpList(list))
                {
                    return list.Select(x => x.PermissionID).ToList();
                }
                return null;
            });
        }

    }
}
