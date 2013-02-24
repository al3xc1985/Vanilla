﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Milkshake.Game.Constants.Game.Update
{
    public enum ObjectUpdateType : byte
    {
        UPDATETYPE_VALUES = 0,
        UPDATETYPE_MOVEMENT = 1,
        UPDATETYPE_CREATE_OBJECT = 2,
        UPDATETYPE_CREATE_OBJECT2 = 3,
        UPDATETYPE_OUT_OF_RANGE_OBJECTS = 4,
        UPDATETYPE_NEAR_OBJECTS = 5
    }
}
