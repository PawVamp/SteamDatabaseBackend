﻿/*
 * Copyright (c) 2013-present, SteamDB. All rights reserved.
 * Use of this source code is governed by a BSD-style license that can be
 * found in the LICENSE file.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SteamKit2;

namespace SteamDatabaseBackend
{
    internal class PackageCommand : Command
    {
        public PackageCommand()
        {
            Trigger = "sub";
            IsSteamCommand = true;
        }

        public override async Task OnCommand(CommandArguments command)
        {
            if (command.Message.Length == 0 || !uint.TryParse(command.Message, out var subID))
            {
                command.Reply("Usage:{0} sub <subid>", Colors.OLIVE);

                return;
            }

            var tokenTask = Steam.Instance.Apps.PICSGetAccessTokens(null, subID);
            tokenTask.Timeout = TimeSpan.FromSeconds(10);
            var tokenCallback = await tokenTask;
            SteamApps.PICSRequest request;

            if (tokenCallback.PackageTokens.ContainsKey(subID))
            {
                request = PICSTokens.NewPackageRequest(subID, tokenCallback.PackageTokens[subID]);
            }
            else
            {
                request = PICSTokens.NewPackageRequest(subID);
            }

            var infoTask = Steam.Instance.Apps.PICSGetProductInfo(Enumerable.Empty<SteamApps.PICSRequest>(), new List<SteamApps.PICSRequest> { request });
            infoTask.Timeout = TimeSpan.FromSeconds(10);
            var job = await infoTask;
            var callback = job.Results?.FirstOrDefault(x => x.Packages.ContainsKey(subID));

            if (callback == null)
            {
                command.Reply("Unknown SubID: {0}{1}{2}", Colors.BLUE, subID, LicenseList.OwnedSubs.ContainsKey(subID) ? SteamDB.StringCheckmark : string.Empty);

                return;
            }

            var info = callback.Packages[subID];

            if (!info.KeyValues.Children.Any())
            {
                command.Reply("No package info returned for SubID: {0}{1}{2}{3}",
                    Colors.BLUE, subID,
                    info.MissingToken ? SteamDB.StringNeedToken : string.Empty,
                    LicenseList.OwnedSubs.ContainsKey(subID) ? SteamDB.StringCheckmark : string.Empty
                );

                return;
            }

            info.KeyValues.SaveToFile(Path.Combine(Application.Path, "sub", string.Format("{0}.vdf", info.ID)), false);

            command.Reply("{0}{1}{2} -{3} {4}{5} - Dump:{6} {7}{8}{9}{10}",
                Colors.BLUE, Steam.GetPackageName(info.ID), Colors.NORMAL,
                Colors.DARKBLUE, SteamDB.GetPackageUrl(info.ID), Colors.NORMAL,
                Colors.DARKBLUE, SteamDB.GetRawPackageUrl(info.ID), Colors.NORMAL,
                info.MissingToken ? SteamDB.StringNeedToken : string.Empty,
                LicenseList.OwnedSubs.ContainsKey(info.ID) ? SteamDB.StringCheckmark : string.Empty
            );
        }
    }
}
