﻿using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Enums;
using Microsoft.Toolkit.Collections;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinstaCore;

namespace Core.Collections.IncrementalSources.Users;

[AddINotifyPropertyChangedInterface]
public class IncrementalUserFollowings : IIncrementalSource<InstaUserShort>
{
    PaginationParameters Pagination { get; set; }
    public long UserId { get; private set; } = -1;

    [OnChangedMethod(nameof(OnSearchQuerryOrOderTypeChanged))]
    public string SearchQuerry { get; set; } = String.Empty;

    [OnChangedMethod(nameof(OnSearchQuerryOrOderTypeChanged))]
    public InstaFollowOrderType OrderType { get; set; } = InstaFollowOrderType.Default;

    public IncrementalUserFollowings(long userId)
    {
        Pagination = PaginationParameters.MaxPagesToLoad(1);
        UserId = userId;
    }

    bool HasMoreAvailable = true;
    public async Task<IEnumerable<InstaUserShort>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!HasMoreAvailable) return new InstaUserShort[0];
        using (IInstaApi Api = AppCore.Container.GetService<IInstaApi>())
        {
            var result = await Api.UserProcessor.GetUserFollowingByIdAsync(UserId, Pagination, cancellationToken,
                searchQuery: SearchQuerry, orderBy: OrderType);
            if (!result.Succeeded)
            {
                if (result.Info.Exception != null && result.Info.Exception is not TaskCanceledException)
                    throw result.Info.Exception;
                else if (result.Info.Exception != null)
                    throw result.Info.Exception;
                else return null;
            }
            HasMoreAvailable = !string.IsNullOrEmpty(result.Value.NextMaxId);
            return result.Value;
        }
    }

    void OnSearchQuerryOrOderTypeChanged()
    {
        Pagination = PaginationParameters.MaxPagesToLoad(1);
        HasMoreAvailable = true;
    }
}
