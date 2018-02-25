﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using Android.App;
using Android.Widget;

namespace AzureStorageBrowser.Activities
{
    [Activity(Label = "Accounts")]
    public class AccountActivity : BaseActivity
    {
        ExpandableListView subscriptionsListView;
        ProgressBar progressBar;

        protected override async void OnCreate(Android.OS.Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Account);

            subscriptionsListView = FindViewById<ExpandableListView>(Resource.Id.subscriptions);
            progressBar = FindViewById<ProgressBar>(Resource.Id.progress);

            try
            {
                var cachedSubscriptions = await BlobCache.LocalMachine.GetObject<Subscription[]>("subscriptions");
                if (cachedSubscriptions != null)
                {
                    subscriptionsListView.SetAdapter(new SubscriptionsListAdapter(this, cachedSubscriptions));
                }
            }
            catch (KeyNotFoundException)
            {
                progressBar.Visibility = Android.Views.ViewStates.Visible;
            }

            subscriptionsListView.SetOnChildClickListener(new AccountClickHandler());

            var token = await BlobCache.LocalMachine.GetObject<string>("token");
            var subscriptions = await FetchSubscriptionsAsync(token);

            await BlobCache.LocalMachine.InsertObject("subscriptions", subscriptions);

            progressBar.Visibility = Android.Views.ViewStates.Gone;

            if (subscriptions.Any() == false)
            {
                var emptyMessage = FindViewById<TextView>(Resource.Id.empty);
                emptyMessage.Visibility = Android.Views.ViewStates.Visible;
            }
            else
            {
                subscriptionsListView.SetAdapter(new SubscriptionsListAdapter(this, subscriptions));
            }
        }

        private async Task<Subscription[]> FetchSubscriptionsAsync(string token)
        {
            var httpClient = new HttpClient();

            var subscriptionResources = await httpClient.GetSubscriptions(token);
            var subscriptions = new List<Subscription>();

            foreach (var subscriptionResource in subscriptionResources)
            {
                var subscription = new Subscription
                {
                    Id = subscriptionResource.SubscriptionId,
                    Name = subscriptionResource.DisplayName,
                    Accounts = await FetchAccountsAsync(subscriptionResource.SubscriptionId, token),
                };

                subscriptions.Add(subscription);
            }

            return subscriptions.ToArray();
        }

        private async Task<Account[]> FetchAccountsAsync(string subscriptionId, string token)
        {
            var httpClient = new HttpClient();

            var resources = await httpClient.GetStorageResources(token, subscriptionId);

            var accounts = await resources.ForEachAsync(async resource =>
            {
                var key = await httpClient.GetStorageKey(token, resource.Id);
                return new Account
                {
                    Name = resource.Name,
                    Id = resource.Id,
                    Key = key,
                };
            });

            return accounts.ToArray();
        }
    }
}
