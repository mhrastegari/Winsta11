﻿using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinstaNext.Core.Collections.IncrementalSources.Search
{
    [AddINotifyPropertyChangedInterface]
    public class IncrementalPeopleSearch : IIncrementalSource<InstaUser>
    {
        [OnChangedMethod(nameof(OnSearchQuerryChanged))]
        public string SearchQuerry { get; set; }

        PaginationParameters pagination { get; set; }

        public IncrementalPeopleSearch()
        {
            pagination = PaginationParameters.MaxPagesToLoad(1);
        }

        bool HasMoreAvailable = true;
        public async Task<IEnumerable<InstaUser>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            if (!HasMoreAvailable) return null;

            using (IInstaApi Api = App.Container.GetService<IInstaApi>())
            {
                var result = await Api.DiscoverProcessor.SearchPeopleAsync(SearchQuerry, pagination);
                if (!result.Succeeded) throw result.Info.Exception;
                HasMoreAvailable = result.Value.HasMoreAvailable;
                return result.Value.Users;
            }
        }

        void OnSearchQuerryChanged()
        {
            pagination = PaginationParameters.MaxPagesToLoad(1);
            HasMoreAvailable = true;
        }
    }

}