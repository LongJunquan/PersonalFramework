﻿using Model;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using System.Web;
//using System.Web.Mvc;
using System.Web.Security;

namespace PersonalFramework.Service
{
    public class UserLoginHelper
    {
        static User _loginuser = null;
        static string cookieName = "PersonalFrameworkPassword_User".ToMD5();
        static string tokenName = "PersonalFrameworkPassword_Token".ToMD5();

        public static User UserLogin(string keyword, string password)
        {
            var context = new DataContext();
            var account = context.Users.Where(x => x.UserName == keyword.Trim()).FirstOrDefault();
            if (account != null)
            {
                var result = DeCrypt.VerifyPassWord(password, account.Password, account.Salt);
                if (!result)
                {
                    return null;
                }
                HttpContext.Current.Session[cookieName] = account;

                HttpCookie cookie = new HttpCookie(cookieName, account.UserName);
                HttpContext.Current.Response.Cookies.Add(cookie);
                HttpCookie cookie2 = new HttpCookie(tokenName, account.UserName.ToMD5()+account.Password);
                HttpContext.Current.Response.Cookies.Add(cookie2);

                return account;
            }
            else
            {
                return null;
            }
        }
        public static string Login(string token)
        {

            #region 存入 Cookie 票据

            // 设置Ticket信息
            FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                 1,
                token,
                DateTime.Now,
                DateTime.Now.AddDays(1),
                false,
                Service.IPHelper.GetClientIp()

                );

            // 加密验证票据
            string strTicket = FormsAuthentication.Encrypt(ticket);

            // 使用新userdata保存cookie
            HttpCookie cookie = new HttpCookie(cookieName, strTicket);
            //cookie.Expires = DateTime.Now.AddMinutes(30);
            cookie.Path = "/";

            HttpContext.Current.Response.Cookies.Add(cookie);
            return strTicket;
            #endregion
        }
        
        //登出
        /// <summary>
        /// 登出
        /// </summary>
        public static void UserLogout()
        {
            if (CurrentUser() !=null)
            {
                //获取ID
                var id = HttpContext.Current.User.Identity.Name;
                FormsAuthentication.SignOut();
                _loginuser = null;
                RemoveUser();
                HttpContext.Current.Session.Remove(cookieName);
            }
        }
        //移除登录缓存
        /// <summary>
        /// 移除的登录缓存
        /// </summary>
        /// <param name="ID"></param>
        public static void RemoveUser()
        {
            HttpCookie cookie = new HttpCookie(cookieName, "");
            cookie.Expires = DateTime.Now.AddYears(-30);
            cookie.Path = "/";
            HttpContext.Current.Response.Cookies.Add(cookie);
            FormsAuthentication.SignOut();
        }
        //获取当前会员登录对象
        /// <summary>
        /// 获取当前会员登录对象
        /// <para>当没登陆或者登录信息不符时，这里返回 null </para>
        /// </summary>
        /// <returns></returns>
        public static Model.User CurrentUser()
        {
            //校验用户是否已经登录
            var user = HttpContext.Current.Session[cookieName] as Model.User;
            if (user != null) return user;
            else
            {
                if (HttpContext.Current.Request.Cookies[cookieName] != null && HttpContext.Current.Request.Cookies[tokenName] != null)
                {
                    string keyword = HttpContext.Current.Request.Cookies[cookieName].Value;
                    string token = HttpContext.Current.Request.Cookies[tokenName].Value;
                    string pwd = token.Substring(32);
                    DataContext context = new DataContext();
                    var account = context.Users.Single(a => a.UserName == keyword.Trim() && a.Password == pwd);
                    if (account != null) return account;
                }
            }
            return null;
        }
    }
}