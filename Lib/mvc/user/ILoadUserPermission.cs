﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.mvc.user
{
    public interface ILoadUserPermission
    {
        List<string> GetUserPermissions(LoginUserInfo user);
    }
}
