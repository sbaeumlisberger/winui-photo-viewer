﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Messages;

public record class ChangeWindowTitleMessage(string NewTitle);