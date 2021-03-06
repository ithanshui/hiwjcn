﻿using Autofac;
using Dal.User;
using Hiwjcn.Core.Infrastructure.Common;
using Hiwjcn.Core.Infrastructure.User;
using Lib.helper;
using Lib.core;
using Lib.extension;
using Lib.io;
using Lib.net;
using Model.User;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using WebLogic.Dal.User;
using WebLogic.Model.User;
using Lib.ioc;
using Lib.infrastructure;
using Lib.mvc.user;

namespace Bll.User
{
    /// <summary>
    /// 用户
    /// </summary>
    public class UserBll : ServiceBase<UserModel>, IUserService, IFetchUserPermissions
    {
        private UserDal _UserDal { get; set; }
        private UserRoleDal _UserRoleDal { get; set; }
        private RoleDal _RoleDal { get; set; }
        private RolePermissionDal _RolePermissionDal { get; set; }

        /// <summary>
        /// 用户逻辑类
        /// </summary>
        public UserBll()
        {
            this._UserDal = new UserDal();
            this._UserRoleDal = new UserRoleDal();
            this._RoleDal = new RoleDal();
            this._RolePermissionDal = new RolePermissionDal();
        }

        public override string CheckModel(UserModel model)
        {
            if (model == null) { return "对象为空"; }
            if (!ValidateHelper.IsPlumpString(model.NickName)) { return "用户昵称不能为空"; }
            if (!(model.Email?.IsEmail() ?? false)) { return "用户邮箱不正确"; }
            if (!ValidateHelper.IsPlumpString(model.PassWord)) { return "用户密码为空"; }
            if (model.Money < 0) { return "账户余额不能小于0"; }

            return string.Empty;
        }

        /// <summary>
        /// 搜索
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public PagerData<UserModel> GetPagerList(
            string name = null, string sex = null, string email = null, string keywords = null,
            bool LoadRoleAndPrivilege = false,
            int page = 1, int pageSize = 20)
        {
            string key = string.Format("SearchUserList:name:{0},sex:{1},email:{2},keywords:{3},page:{4},pagesize:{5},"
                + "LoadRoleAndPrivilege:{6}",
                name, sex, email, keywords, page, pageSize, LoadRoleAndPrivilege);

            return Cache(key, () =>
            {
                var data = new PagerData<UserModel>();

                #region 查询
                _UserDal.PrepareIQueryable((query) =>
                {
                    if (ValidateHelper.IsPlumpString(name))
                    {
                        query = query.Where(x => x.NickName == name);
                        data.UrlParams["name"] = name;
                    }

                    if (ValidateHelper.IsPlumpString(sex))
                    {
                        query = query.Where(x => x.Sex == sex);
                        data.UrlParams["sex"] = name;
                    }

                    if (ValidateHelper.IsPlumpString(email))
                    {
                        query = query.Where(x => x.Email == email);
                        data.UrlParams["email"] = name;
                    }

                    if (ValidateHelper.IsPlumpString(keywords))
                    {
                        query = query.Where(x =>
                            x.NickName.Contains(keywords)
                            || x.Phone.Contains(keywords)
                            || x.Email.Contains(keywords)
                            || x.Introduction.Contains(keywords)
                            || x.QQ.Contains(keywords));
                        data.UrlParams["q"] = keywords;
                    }

                    data.ItemCount = query.Count();
                    data.DataList = query.OrderByDescending(x => x.UserID).QueryPage(page, pageSize).ToList();
                    return true;
                });
                #endregion

                if (ValidateHelper.IsPlumpList(data?.DataList) && LoadRoleAndPrivilege)
                {
                    data.DataList = GetRolesForUserList(data.DataList);
                }

                return data;
            });
        }

        /// <summary>
        /// 从数据库里读取用户头像
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public byte[] GetUserImage(int userID)
        {
            string key = "userbll.getuserimage:" + userID;
            return Cache(key, () =>
            {
                var b = _UserDal.ReadUserImage(userID);
                //最好在这里压缩一下
                return b;
            });
        }

        /// <summary>
        /// 通过id获取user
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public UserModel GetByID(int userID)
        {
            var list = GetUserByIDS(userID);
            if (ValidateHelper.IsPlumpList(list))
            {
                return list[0];
            }
            return null;
        }

        /// <summary>
        /// 通过id获取多个user对象
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<UserModel> GetUserByIDS(params int[] ids)
        {
            if (ids == null) { return null; }
            ids = ids.Where(x => x > 0).ToArray();
            if (!ValidateHelper.IsPlumpList(ids)) { return null; }
            string key = "userbll.userbyids:" + string.Join(",", ids);
            return Cache(key, () =>
            {
                return _UserDal.GetList(x => ids.Contains(x.UserID));
            });
        }

        /// <summary>
        /// 用户数按照性别分组统计
        /// </summary>
        /// <returns></returns>
        public List<UserCountGroupBySex> GetCountGroupBySex()
        {
            string key = "userbll.GetCountGroupBySex";
            return Cache(key, () =>
            {
                List<UserCountGroupBySex> data = null;
                _UserDal.PrepareIQueryable((query) =>
                {
                    data = query.GroupBy(x => x.Sex)
                        .Select(x => new UserCountGroupBySex() { Sex = x.Key, Count = x.Count() })
                        .OrderBy(x => x.Sex).Skip(0).Take(4).ToList();

                    data.ForEach(x =>
                    {
                        x.Sex = UserModel.ParseSex(x.Sex);
                    });

                    return true;
                });
                return data;
            });
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public string DeleteUser(int userID)
        {
            var model = _UserDal.GetByKeys(userID);
            if (model == null) { return "你要删除的用户不存在"; }
            return _UserDal.Delete(model) > 0 ? SUCCESS : "删除失败";
        }

        /// <summary>
        /// 更新用户头像
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="file"></param>
        /// <param name="MaxSize"></param>
        /// <returns></returns>
        public string UpdateUserMask(int userID, HttpPostedFile file, string save_path)
        {
            if (file == null || file.InputStream == null)
            {
                return "读取不到文件";
            }
            if (!Directory.Exists(save_path))
            {
                return "存储目录不存在";
            }

            var uploader = new FileUpload();
            uploader.AllowFileType = new string[] { ".gif", ".png", ".jpg", ".jpeg" };
            uploader.MaxSize = Com.MbToB(0.5f);
            var model = uploader.UploadSingleFile(file, save_path);

            if (!model.SuccessUpload || !File.Exists(model.FilePath)) { return model.Info; }

            var b = IOHelper.GetFileBytes(model.FilePath);
            if (!ValidateHelper.IsPlumpList(b))
            {
                return "本地文件丢失";
            }

            var errmsg = new List<string>();

            #region 把文件保存到数据库
            if (_UserDal.UpdateUserMask(userID, b) <= 0) { errmsg.Add("保存字节数组失败"); }
            #endregion

            #region 把文件保存到云存储
            var file_url = string.Empty;
            var file_name = string.Empty;
            var upfileservice = AppContext.GetObject<IUpFileService>();
            var qiniumsg = upfileservice.UploadFileAfterCheckRepeat(new FileInfo(model.FilePath), userID, ref file_url, ref file_name);
            if (ValidateHelper.IsPlumpString(qiniumsg)) { errmsg.Add(qiniumsg); }
            else
            {
                if (ValidateHelper.IsPlumpString(file_url))
                {
                    //保存文件地址到数据库
                    var updatemsg = UpdateSingleEntity(x => x.UserID == userID, (ref UserModel entity) =>
                    {
                        entity.UserImg = file_url;
                        return string.Empty;
                    });
                    if (ValidateHelper.IsPlumpString(updatemsg)) { errmsg.Add(updatemsg); }
                }
                else
                {
                    errmsg.Add("文件地址为空");
                }
            }
            return errmsg.Count() == 0 ? SUCCESS : string.Join(",", errmsg);
            #endregion
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="nick_name"></param>
        /// <param name="sex"></param>
        /// <param name="phone"></param>
        /// <param name="qq"></param>
        /// <param name="introduction"></param>
        /// <returns></returns>
        public string UpdateUserInfo(UserModel updateModel)
        {
            var model = _UserDal.GetByKeys(updateModel.UserID);
            if (model == null) { return "用户不存在"; }

            model.NickName = updateModel.NickName;
            model.Sex = updateModel.Sex;
            model.Phone = updateModel.Phone;
            model.QQ = updateModel.QQ;
            model.Introduction = updateModel.Introduction;

            string errinfo = CheckModel(model);
            if (ValidateHelper.IsPlumpString(errinfo))
            {
                return errinfo;
            }

            return _UserDal.Update(model) > 0 ? SUCCESS : "更新失败";
        }

        /// <summary>
        /// 修改用户密码
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public string UpdateUserPass(int userID, string old_pass, string new_pass, string re_new_pass)
        {
            if (!ValidateHelper.IsAllPlumpString(old_pass, new_pass)) { return "请输入旧密码和新密码"; }
            if (new_pass != re_new_pass) { return "两次输入的新密码不相同"; }

            string md5pass = UserPassWordEncrypt(old_pass);
            var model = _UserDal.GetFirst(x => x.UserID == userID && x.PassWord == md5pass);
            if (model == null) { return "用户不存在"; }
            model.PassWord = UserPassWordEncrypt(new_pass);

            string errinfo = CheckModel(model);
            if (ValidateHelper.IsPlumpString(errinfo))
            {
                return errinfo;
            }

            return _UserDal.Update(model) > 0 ? SUCCESS : "修改密码失败";
        }

        /// <summary>
        /// 加密用户密码
        /// </summary>
        /// <param name="originalPWD"></param>
        /// <returns></returns>
        private string UserPassWordEncrypt(string originalPWD)
        {
            return SecureHelper.GetMD5(originalPWD);
        }

        /// <summary>
        /// 生成token
        /// </summary>
        /// <param name="model"></param>
        /// <param name="sms"></param>
        /// <returns></returns>
        public string CreateToken(UserModel model)
        {
            return Com.GetPassKey($"{model.UserID}/{model.UID}/{model.PassWord}");
        }
        private UserModel FindValidLoginUser(string email, ref string msg)
        {
            var loginemail = ConvertHelper.GetString(email).Trim().ToLower();
            var model = _UserDal.GetFirst(x => x.Email == loginemail);
            if (model == null)
            {
                msg = "此用户不存在";
                return null;
            }

            if (model.Flag < 0)
            {
                msg = "您的账户被冻结，请联系管理员";
                return null;
            }

            //获取用户权限
            model = LoadAllPermissionForUser(model);
            //生成token
            model.UserToken = CreateToken(model);
            return model;
        }

        /// <summary>
        /// 登录账户并记录登录状态
        /// </summary>
        /// <param name="email"></param>
        /// <param name="pass"></param>
        /// <param name="autoLogin"></param>
        /// <returns></returns>
        public UserModel LoginByPassWord(string email, string pass, ref string msg)
        {
            if (!ValidateHelper.IsLenInRange(email, 5, 50) || !ValidateHelper.IsLenInRange(pass, 5, 50))
            {
                msg = "请输入正确的账户密码";
                return null;
            }
            var loginuser = FindValidLoginUser(email, ref msg);
            if (loginuser != null)
            {
                if (loginuser.PassWord != UserPassWordEncrypt(pass))
                {
                    msg = "密码错误";
                    return null;
                }
            }
            return loginuser;
        }

        /// <summary>
        /// 通过token找到用户
        /// </summary>
        /// <param name="email"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public UserModel LoginByToken(string email, string token)
        {
            var msg = string.Empty;
            var loginuser = FindValidLoginUser(email, ref msg);
            if (loginuser != null)
            {
                if (loginuser.UserToken != token)
                {
                    msg = "token错误";
                    return null;
                }
            }
            return loginuser;
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="model"></param>
        /// <param name="web_name"></param>
        /// <returns></returns>
        public string Register(UserModel model, string web_name = null)
        {
            model.UID = Com.GetUUID();
            model.Email = ConvertHelper.GetString(model.Email).Trim().ToLower();
            model.PassWord = ConvertHelper.GetString(model.PassWord).Trim();

            string errinfo = CheckModel(model);
            if (ValidateHelper.IsPlumpString(errinfo)) { return errinfo; }

            //检查用户是否存在
            if (_UserDal.Exist(x => x.Email == model.Email))
            {
                return "您输入的邮箱已经存在";
            }
            //添加用户
            if (_UserDal.Add(model) <= 0)
            {
                return "注册失败";
            }
            //保险起见，读取一下刚刚写入的用户信息
            model = _UserDal.GetFirst(x => x.Email == model.Email);
            if (model == null)
            {
                return "注册验证失败";
            }
            //发送邮件
            errinfo = SendEmail(model, web_name);
            if (ValidateHelper.IsPlumpString(errinfo))
            {
                return errinfo;
            }
            return SUCCESS;
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="model"></param>
        /// <param name="web_name"></param>
        /// <returns></returns>
        private string SendEmail(UserModel model, string web_name)
        {
            try
            {
                var mail = new EmailModel();
                mail.ToList = new List<string>() { model.Email };
                mail.Subject = string.Format("恭喜你注册成功，初始登陆帐户：{0}", model.Email);
                if (ValidateHelper.IsPlumpString(web_name))
                {
                    mail.Subject = string.Format("{0}提示：", web_name) + mail.Subject;
                }
                mail.MailBody = mail.Subject;
                if (!EmailSender.SendMail(mail))
                {
                    return "账号注册成功，但是未能发送到您的邮箱";
                }
                return SUCCESS;
            }
            catch (Exception e)
            {
                e.AddLog(this.GetType());
                return "账号注册成功，但是邮件发送异常";
            }
        }

        /// <summary>
        /// 重设用户密码为随机密码，并将新密码发送到用户邮箱
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public string ResetUserPassWord(string email)
        {
            if (!ValidateHelper.IsEmail(email))
            {
                return "请输入注册邮箱";
            }
            var model = _UserDal.GetFirst(x => x.Email == email);
            if (model == null) { return "用户不存在"; }
            string user_pass = Com.GetRandomNumString();
            model.PassWord = UserPassWordEncrypt(user_pass);

            string errinfo = CheckModel(model);
            if (ValidateHelper.IsPlumpString(errinfo)) { return errinfo; }

            if (_UserDal.Update(model) <= 0)
            {
                return "密码修改错误";
            }

            try
            {
                var mail = new EmailModel();
                mail.ToList = new List<string>() { email };
                mail.Subject = string.Format("您申请了重设密码，目前登陆帐户：{0}，密码：{1}", email, user_pass);
                mail.MailBody = mail.Subject;
                if (!EmailSender.SendMail(mail))
                {
                    return "发送邮件错误";
                }
            }
            catch (Exception e)
            {
                e.AddLog(this.GetType());
            }

            return SUCCESS;
        }

        /// <summary>
        /// 读取用户角色和用户特权（读取model）
        /// </summary>
        /// <param name="list"></param>
        /// <param name="LoadEntity"></param>
        /// <returns></returns>
        private List<UserModel> GetRolesForUserList(List<UserModel> list)
        {
            var useridlist = list.Select(x => x.UserID).ToArray();

            //读取角色
            var userrolemaplist = _UserRoleDal.GetList(x => useridlist.Contains(x.UserID));
            if (ValidateHelper.IsPlumpList(userrolemaplist))
            {
                var roleidlist = userrolemaplist.Select(x => x.RoleID).Distinct().ToArray();
                var rolelist = _RoleDal.GetList(x => roleidlist.Contains(x.RoleID));
                if (ValidateHelper.IsPlumpList(rolelist))
                {
                    foreach (var model in list)
                    {
                        var myroleidlist = userrolemaplist.Where(x => x.UserID == model.UserID).Select(x => x.RoleID).Distinct().ToArray();
                        if (!ValidateHelper.IsPlumpList(myroleidlist)) { continue; }
                        model.RoleModelList = rolelist.Where(x => myroleidlist.Contains(x.RoleID)).ToList();
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// 获取用户的所有权限（只读取ID）
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private UserModel LoadAllPermissionForUser(UserModel model)
        {
            if (model == null || model.UserID <= 0) { throw new Exception("用户对象为空，无法为其获取权限"); }
            model.RoleList = new List<int>();
            model.PermissionList = new List<string>();

            _UserDal.PrepareSession(db =>
            {
                var maprole = db.Set<UserRoleModel>().Where(x => x.UserID == model.UserID).Select(x => x.RoleID);
                var deftrole = db.Set<RoleModel>().Where(x => x.AutoAssignRole == "true").Select(x => x.RoleID);

                var rolepermissionlist = db.Set<RolePermissionModel>()
                .Where(x => maprole.Contains(x.RoleID) || deftrole.Contains(x.RoleID))
                .Select(x => new { role = x.RoleID, permission = x.PermissionID }).ToList();

                if (ValidateHelper.IsPlumpList(rolepermissionlist))
                {
                    model.RoleList.AddRange(rolepermissionlist.Select(x => x.role).Distinct());
                    model.PermissionList.AddRange(rolepermissionlist.Select(x => x.permission).Distinct());
                }
                return true;
            });

            return model;
        }

        /// <summary>
        /// 获取用户权限
        /// </summary>
        /// <param name="loginuser"></param>
        /// <returns></returns>
        public List<string> FetchPermission(LoginUserInfo loginuser)
        {
            var list = Cache($"user_permissions_in_cache:{loginuser.UserID}", () =>
            {
                var RoleList = new List<int>();
                var PermissionList = new List<string>();
                _UserDal.PrepareSession(db =>
                {
                    var maprole = db.Set<UserRoleModel>().Where(x => x.UserID == loginuser.IID).Select(x => x.RoleID);
                    var deftrole = db.Set<RoleModel>().Where(x => x.AutoAssignRole == "true").Select(x => x.RoleID);

                    var rolepermissionlist = db.Set<RolePermissionModel>()
                    .Where(x => maprole.Contains(x.RoleID) || deftrole.Contains(x.RoleID))
                    .Select(x => new { role = x.RoleID, permission = x.PermissionID }).ToList();

                    if (ValidateHelper.IsPlumpList(rolepermissionlist))
                    {
                        RoleList.AddRange(rolepermissionlist.Select(x => x.role).Distinct());
                        PermissionList.AddRange(rolepermissionlist.Select(x => x.permission).Distinct());
                    }
                    return true;
                });
                return PermissionList;
            });
            return ConvertHelper.NotNullList(list);
        }
    }
}
