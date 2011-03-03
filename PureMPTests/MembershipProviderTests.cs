﻿using System;
using System.Collections.Generic;
using System.Web.Security;
using NUnit.Framework;


namespace PureDev.Common
{

    [TestFixture]
    public class MembershipProviderTests
    {
        MembershipProvider mp;
        const string domain = "puredev.eu";
        const string specialUN = "malinek";
        const string specialPSW = "malinkapralinka";

        protected string ProviderName = "PureMembershipProvider";

        [TestFixtureSetUp]
        public void Init()
        {
            try
            {
                mp = Membership.Providers[ProviderName];
                CreateSpecialUser();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        [Test]
        public void CreateSpecialUser()
        {
            string user = specialUN;
            var muser = CreateUser(user, specialPSW, user + "@" + domain, true);
            if (muser == null)
            {
                muser = mp.GetUser(user, false);
            }

            Assert.IsNotNull(muser);
            Assert.AreEqual(muser.Email, user + "@" + domain);
            Assert.AreEqual(muser.UserName, user);
        }

        [Test]
        public void TestCreatingUsers()
        {
            string user = Guid.NewGuid().ToString().Substring(0, 12);
            var muser = CreateUser(user, "lalagugu1", user + "@" + domain, true);
            Assert.IsNotNull(muser);
            Assert.AreEqual(muser.Email, user + "@" + domain);
            Assert.AreEqual(muser.UserName, user);
            Assert.IsTrue(mp.DeleteUser(user, true));
            Assert.IsNull(mp.GetUser(user, false));
        }

        [Test]
        public void TestLoginSucc()
        {
            if (mp.GetUser(specialUN, false) == null)
                CreateSpecialUser();

            var logged = mp.ValidateUser(specialUN, specialPSW);
            Assert.IsTrue(logged);

            var user = mp.GetUser(specialUN, false);
            Assert.IsNotNull(user);
            Assert.AreEqual(user.LastLoginDate.Date, DateTime.Today, "Last login date is wrong!");
            Assert.AreEqual(user.LastLoginDate.Hour, DateTime.Now.Hour, "Last login hour is wrong!");
            Assert.AreEqual(user.LastLoginDate.Minute, DateTime.Now.Minute, "Last login Minute is wrong!");
        }

        [Test]
        public void TestLoginWrongPsw()
        {
            var logged = mp.ValidateUser(specialUN, specialPSW + "1");
            Assert.IsFalse(logged);

        }

        [Test]
        public void TestLoginWrongUser()
        {
            var logged = mp.ValidateUser(specialUN + "111", specialPSW);
            Assert.IsFalse(logged);
        }

        [Test]
        public void TestLoginInPerformance()
        {
            DateTime now = DateTime.Now;
            for (int i = 0; i < 1000; i++)
            {
                var logged = mp.ValidateUser(specialUN + "ss", specialPSW);
                Assert.IsFalse(logged);
            }
            Console.WriteLine("{0} login attempts took {1}", 1000, (DateTime.Now - now));
        }

        [Test]
        public void Test10LoginWrongPsw()
        {
            var user = mp.GetUser(specialUN, false);
            Assert.IsNotNull(user);
            for(int i=0;i<mp.MaxInvalidPasswordAttempts + 5;i++)
            {
                var logged = mp.ValidateUser(specialUN, specialPSW + "1");
                Assert.IsFalse(logged);
                user = mp.GetUser(specialUN, false);
                Assert.IsNotNull(user);
                if (user.IsLockedOut)
                {
                    Assert.Pass("User has been locked after " + i + " unsuccessfull tries!");
                    return;
                }
            }
            Assert.Fail("User hasn't been locked after {0} failed tries", mp.MaxInvalidPasswordAttempts + 5);
        }

        [Test]
        public void ChangePswTest()
        {
            var user = mp.GetUser(specialUN, false);
            Assert.IsNotNull(user);
            user.ChangePassword(specialPSW, specialPSW + "lala");
            Assert.IsFalse(mp.ValidateUser(specialUN, specialPSW));
            Assert.IsTrue(mp.ValidateUser(specialUN, specialPSW + "lala"));
            user.ChangePassword(specialPSW + "lala", specialPSW);
            Assert.IsTrue(mp.ValidateUser(specialUN, specialPSW));
            Assert.IsFalse(mp.ValidateUser(specialUN, specialPSW + "lala"));
        }

        [Test]
        public void TestGetUsersByEmail()
        {
            var user = mp.GetUser(specialUN, false);
            if(user == null)
                CreateSpecialUser();
            int lala;
            var users = mp.FindUsersByEmail(specialUN + "@" + domain, 0, 10, out lala);
            Assert.IsTrue(users.Count > 0);
            Assert.AreEqual(lala, users.Count);
        }

        [Test]
        public void UnlockUserTest()
        {
            var user = mp.GetUser(specialUN, false);
            Assert.IsNotNull(user);
            if (!user.IsLockedOut)
                Test10LoginWrongPsw();
            user = mp.GetUser(specialUN, false);
            Assert.IsNotNull(user);
            Assert.IsTrue(user.IsLockedOut);

            user.UnlockUser();
            user = mp.GetUser(specialUN, false);
            Assert.IsNotNull(user);
            Assert.IsFalse(user.IsLockedOut);
        }

        [Test]
        public void OnlineUsersTest()
        {
            var user = mp.GetUser(specialUN, true);
            Assert.IsNotNull(user);
            Assert.IsTrue(mp.GetNumberOfUsersOnline() > 0);
        }

        [Test]
        public void TestCreatingUsers2()
        {
            string user = Guid.NewGuid().ToString().Substring(0, 12);
            var muser = CreateUser(user, "lalagugu1", user + "@" + domain, true);
            Assert.IsNotNull(muser);
            Assert.AreEqual(muser.Email, user + "@" + domain);
            Assert.AreEqual(muser.UserName, user);

            var myuser = mp.GetUser(user, false) as MembershipUser;
            Assert.IsNotNull(myuser);
            //Assert.IsNotNull(mp.GetUser(myuser.id_user, false));

            Assert.IsTrue(mp.DeleteUser(user, true));
            Assert.IsNull(mp.GetUser(user, false));
        }


        private MembershipUser CreateUser(string userName, string psw, string email, bool approved)
        {
            MembershipCreateStatus mcp;
            var user = mp.CreateUser(userName, psw, email, "Frącwa", "Kuźmielińska", true, Guid.NewGuid(), out mcp);
            Console.WriteLine("Creating user result: {0}", mcp.ToString() );
            return user;
        }

        [Test]
        public void HeavyCreatingUsersTest()
        {
            const int PERF_COUNT = 500;
            var users = new List<string>(PERF_COUNT);
            DateTime now = DateTime.Now;
            for (int i = 0; i < PERF_COUNT; i++)
            {
                string userName = Guid.NewGuid().ToString().Replace("-", "");
                CreateUser(userName, "", userName + "@" + "@puredevb.eu", true);
                users.Add(userName);
            }
            Console.WriteLine("Creating {0} users took {1}", PERF_COUNT, (DateTime.Now - now));
            now = DateTime.Now;
            for (int i = 0; i < PERF_COUNT; i++)
            {
                mp.DeleteUser(users[i], true);
            }
            Console.WriteLine("Deleting {0} users took {1}", PERF_COUNT, (DateTime.Now - now));
        }

        [Test]
        public void ResetPasswordTest()
        {
            var user = mp.GetUser(specialUN, false);
            Assert.IsNotNull(user);
            if(user.IsLockedOut)
            {
                UnlockUserTest();
            }
            string newPsw = user.ResetPassword(null);
            Assert.IsTrue(mp.ValidateUser(specialUN, newPsw));
            Assert.IsFalse(mp.ValidateUser(specialUN, specialPSW));
            user.ChangePassword(newPsw, specialPSW);
            Assert.IsTrue(mp.ValidateUser(specialUN, specialPSW));
            Assert.IsFalse(mp.ValidateUser(specialUN, newPsw));
        }
    }
}
