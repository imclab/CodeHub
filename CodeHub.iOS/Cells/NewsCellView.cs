using System;
using Foundation;
using UIKit;
using CodeHub.iOS;
using ReactiveUI;
using System.Reactive.Linq;
using CodeHub.Core.ViewModels.Activity;
using Humanizer;
using System.Collections.Generic;
using CoreText;
using System.Text;

namespace CodeHub.iOS.Cells
{
    public partial class NewsCellView : ReactiveTableViewCell<EventItemViewModel>
    {
        public static readonly UINib Nib = UINib.FromName("NewsCellView", NSBundle.MainBundle);
        public static readonly NSString Key = new NSString("NewsCellView");
        public static readonly UIEdgeInsets EdgeInsets = new UIEdgeInsets(0, 48f, 0, 0);
        public static UIColor LinkColor = Theme.MainTitleColor;

        public static UIFont LinkFont = UIFont.FromDescriptor(
            UIFont.PreferredSubheadline.FontDescriptor.CreateWithTraits(UIFontDescriptorSymbolicTraits.Bold), 
            UIFont.PreferredSubheadline.PointSize); 

        private static IDictionary<EventItemViewModel.EventType, Func<UIImage>> _eventToImage 
            = new Dictionary<EventItemViewModel.EventType, Func<UIImage>>
        {
            {EventItemViewModel.EventType.Unknown, () => Images.Alert},
            {EventItemViewModel.EventType.Branch, () => Images.Branch},
            {EventItemViewModel.EventType.Comment, () => Images.Comment},
            {EventItemViewModel.EventType.Commit, () => Images.Commit},
            {EventItemViewModel.EventType.Delete, () => Images.Trashcan},
            {EventItemViewModel.EventType.Follow, () => Images.Following},
            {EventItemViewModel.EventType.Fork, () => Images.Fork},
            {EventItemViewModel.EventType.Gist, () => Images.Gist},
            {EventItemViewModel.EventType.Issue, () => Images.IssueOpened},
            {EventItemViewModel.EventType.Organization, () => Images.Organization},
            {EventItemViewModel.EventType.Public, () => Images.Globe},
            {EventItemViewModel.EventType.PullRequest, () => Images.PullRequest},
            {EventItemViewModel.EventType.Repository, () => Images.Repo},
            {EventItemViewModel.EventType.Star, () => Images.Star},
            {EventItemViewModel.EventType.Tag, () => Images.Tag},
            {EventItemViewModel.EventType.Wiki, () => Images.Pencil},
        };

        public class Link
        {
            public NSRange Range;
            public Action Callback;
            public int Id;
        }

        public static NewsCellView Create()
        {
            return Nib.Instantiate(null, null).GetValue(0) as NewsCellView;
        }

        public NewsCellView(IntPtr handle) 
            : base(handle)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            Image.Layer.MasksToBounds = true;
            Image.Layer.CornerRadius = Image.Bounds.Height / 2f;
            ContentView.Opaque = true;
            SeparatorInset = EdgeInsets;
            ActionImage.TintColor = Time.TextColor;

            Header.EnabledTextCheckingTypes = MonoTouch.TTTAttributedLabel.NSTextCheckingTypes.NSTextCheckingTypeLink;
            Body.EnabledTextCheckingTypes = MonoTouch.TTTAttributedLabel.NSTextCheckingTypes.NSTextCheckingTypeLink;

            Header.LinkAttributes = new CTStringAttributes {
                Font = new CTFont(LinkFont.Name, LinkFont.PointSize),
                ForegroundColor = LinkColor.CGColor
            }.Dictionary;

            Body.LinkAttributes = new CTStringAttributes {
                Font = new CTFont(UIFont.PreferredSubheadline.Name, UIFont.PreferredSubheadline.PointSize),
                ForegroundColor = LinkColor.CGColor
            }.Dictionary;

            Header.ActiveLinkAttributes = new CTStringAttributes {
                Font = new CTFont(LinkFont.Name, LinkFont.PointSize),
                ForegroundColor = LinkColor.CGColor,
                UnderlineStyle = CTUnderlineStyle.Single
            }.Dictionary;

            Body.ActiveLinkAttributes = new CTStringAttributes {
                Font = new CTFont(UIFont.PreferredSubheadline.Name, UIFont.PreferredSubheadline.PointSize),
                ForegroundColor = LinkColor.CGColor,
                UnderlineStyle = CTUnderlineStyle.Single
            }.Dictionary;

            this.WhenAnyValue(x => x.ViewModel)
                .IsNotNull()
                .Subscribe(x =>
                {
                    Image.SetAvatar(x.Avatar);
                    Time.Text = x.Created.UtcDateTime.Humanize();

                    ActionImage.Image = _eventToImage.ContainsKey(x.Type) ? 
                        _eventToImage[x.Type]().ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate) : 
                        Images.Alert.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);

                    List<NewsCellView.Link> headerLinks;
                    Header.Text = CreateAttributedStringFromBlocks(x.HeaderBlocks, out headerLinks);
                    Header.Delegate = new LabelDelegate(headerLinks, w => {});

                    List<NewsCellView.Link> bodyLinks;
                    Body.Text = CreateAttributedStringFromBlocks(x.BodyBlocks, out bodyLinks);
                    Body.Delegate = new LabelDelegate(bodyLinks, w => {});

                    foreach (var b in headerLinks)
                        Header.AddLinkToURL(new NSUrl(b.Id.ToString()), b.Range);

                    foreach (var b in bodyLinks)
                        Body.AddLinkToURL(new NSUrl(b.Id.ToString()), b.Range);
                });

        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            ContentView.SetNeedsLayout();
            ContentView.LayoutIfNeeded();
            Header.PreferredMaxLayoutWidth = Header.Frame.Width;
            Body.PreferredMaxLayoutWidth = Body.Frame.Width;
        }

        class LabelDelegate : MonoTouch.TTTAttributedLabel.TTTAttributedLabelDelegate
        {

            private readonly List<Link> _links;
            private readonly Action<NSUrl> _webLinkClicked;

            public LabelDelegate(List<Link> links, Action<NSUrl> webLinkClicked)
            {
                _links = links;
                _webLinkClicked = webLinkClicked;
            }

            public override void DidSelectLinkWithURL (MonoTouch.TTTAttributedLabel.TTTAttributedLabel label, NSUrl url)
            {
                try
                {
                    if (url.ToString().StartsWith("http", StringComparison.Ordinal))
                    {
                        if (_webLinkClicked != null)
                            _webLinkClicked(url);
                    }
                    else
                    {
                        var i = Int32.Parse(url.ToString());
                        _links[i].Callback();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to callback on TTTAttributedLabel: {0}", e.Message);
                }
            }
        }

        private static NSString CreateAttributedStringFromBlocks(
                IEnumerable<BaseEventsViewModel.TextBlock> blocks,
                out List<NewsCellView.Link> links)
        {
            var sb = new StringBuilder();
            links = new List<NewsCellView.Link>();

            int lengthCounter = 0;
            int i = 0;

            foreach (var b in blocks)
            {
                sb.Append(b.Text);
                var strLength = b.Text.Length;

                var anchorBlock = b as BaseEventsViewModel.AnchorBlock;
                if (anchorBlock != null)
                    links.Add(new NewsCellView.Link { Range = new NSRange(lengthCounter, strLength), Callback = anchorBlock.Tapped, Id = i++ });
                lengthCounter += strLength;
            }

            return new NSString(sb.ToString());
        }
    }
}

