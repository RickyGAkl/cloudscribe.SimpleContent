﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:                  Joe Audette
// Created:                 2016-02-17
// Last Modified:           2016-03-20
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cloudscribe.SimpleContent.Storage.Json
{
    public interface IJsonPersister
    {
        Task DeletePageFile(string projectId, string pageId);
        
        Task SavePageFile(string projectId, string pageId, string json);
    }
}
