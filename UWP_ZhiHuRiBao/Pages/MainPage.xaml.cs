﻿using LLQ;
using Brook.ZhiHuRiBao.Events;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Brook.ZhiHuRiBao.Models;
using Brook.ZhiHuRiBao.Common;
using XPHttp;
using Windows.UI.Core;
using Brook.ZhiHuRiBao.Utils;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.ViewManagement;
using Brook.ZhiHuRiBao.ViewModels;
using System;
using WeiboSDKForWinRT;
using Windows.UI.Popups;
using Brook.ZhiHuRiBao.Authorization;
using Windows.UI.Xaml.Media.Imaging;

namespace Brook.ZhiHuRiBao.Pages
{
    public sealed partial class MainPage : PageBase
    {
        public MainViewModel VM { get { return GetVM<MainViewModel>(); } }

        public bool IsDesktopDevice { get { return UIViewSettings.GetForCurrentView().UserInteractionMode == UserInteractionMode.Mouse; } }

        public bool IsCommentPanelOpen { get { return StorageUtil.StorageInfo.IsCommentPanelOpen; } }

        public MainPage()
        {
            this.InitializeComponent();
            Initalize();
            NavigationCacheMode = NavigationCacheMode.Required;

            MainListView.Refresh = RefreshMainList;
            MainListView.LoadMore = LoadMoreStories;

            Loaded += MainPage_Loaded;

            MyFav.Content = StringUtil.GetString("Favorite");
            DownloadButton.Content = StringUtil.GetString("DownloadOffline");
            MainListView.SetRefresh(true);

            LLQNotifier.Default.Register(this);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

            AuthorizationHelper.AutoLogin(VM.LoginSuccess);
        }

        private async void RefreshMainList()
        {
            await VM.Refresh();
            MainListView.SetRefresh(false);
            if (!Config.IsSinglePage)
            {
                DisplayStory(ViewModelBase.CurrentStoryId);
            }
        }

        private async void LoadMoreStories()
        {
            await VM.LoadMore();
            MainListView.FinishLoadingMore();
        }

        private void MainListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var story = e.ClickedItem as Story;
            if (Misc.IsGroupItem(story.type))
                return;

            var storyId = story.id.ToString();
            DisplayStory(storyId);
        }

        private void TapFlipImage(object sender, RoutedEventArgs e)
        {
            var storyId = (sender as FrameworkElement).Tag.ToString();
            if (storyId != Misc.Unvalid_Image_Id.ToString())
            {
                DisplayStory(storyId);
            }
        }

        private void DisplayStory(string storyId)
        {
            ViewModelBase.CurrentStoryId = storyId;
            if(Config.UIStatus == AppUIStatus.All || Config.UIStatus == AppUIStatus.ListAndContent)
            {
                MainContentFrame.Navigate(typeof(MainContentPage), storyId);
                CommentFrame.Navigate(typeof(CommentPage), storyId);
            }
            else
            {
                ((Frame)Window.Current.Content).Navigate(typeof(MainContentPage), storyId);
            }
        }

        private void CategoryListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var category = e.ClickedItem as Others;
            VM.CurrentCategoryId = category.id;
            VM.CategoryName = category.name;
            MainListView.SetRefresh(true);
            ResetCategoryPanel();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(Config.IsPageSwitched(e.PreviousSize, e.NewSize) && !string.IsNullOrEmpty(ViewModelBase.CurrentStoryId))
            {
                MainContentFrame.Navigate(typeof(MainContentPage));
                CommentFrame.Navigate(typeof(CommentPage));
            }
        }

        [SubscriberCallback(typeof(DefaultEvent))]
        private void Subscriber(DefaultEvent param)
        {
            switch(param.EventType)
            {
                case EventType.ClickMenu:
                    ResetCategoryPanel();
                    break;
                case EventType.ClickComment:
                    if (!Config.IsSinglePage)
                    {
                        StoryContentView.IsPaneOpen = !StoryContentView.IsPaneOpen;
                    }

                    if(Config.UIStatus == AppUIStatus.All)
                    {
                        StorageUtil.SetCommentPanelStatus(StoryContentView.IsPaneOpen);
                    }
                    break;
            }
        }

        private void Login(object sender, RoutedEventArgs e)
        {
            AuthorizationHelper.Login(LoginType.Sina, (isSuccess, msg) =>
            {
                VM.LoginSuccess();
            });
            ResetCategoryPanel();
        }

        private void ResetCategoryPanel()
        {
            MainView.IsPaneOpen = !MainView.IsPaneOpen;
        }
    }
}
