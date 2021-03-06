using System;
using CodeHub.Core.ViewModels.App;
using System.Reactive.Linq;
using UIKit;
using CoreGraphics;
using CodeHub.iOS.ViewComponents;
using ReactiveUI;
using CodeHub.iOS.DialogElements;
using CodeHub.iOS.TableViewSources;

namespace CodeHub.iOS.Views.App
{
    public class FeedbackComposerView : BaseTableViewController<FeedbackComposerViewModel>
    {
        private readonly InputElement _titleElement = new DummyInputElement("Title");
        private readonly ExpandingInputElement _descriptionElement;

        public FeedbackComposerView()
        {
            _descriptionElement = new ExpandingInputElement("Description");
            _descriptionElement.AccessoryView = x =>
                new MarkdownAccessoryView(x, ViewModel.PostToImgurCommand) { Frame = new CGRect(0, 0, 320f, 44f) };

            this.WhenViewModel(x => x.Subject).Subscribe(x => _titleElement.Value = x);
            _titleElement.Changed += (sender, e) => ViewModel.Subject = _titleElement.Value;

            this.WhenViewModel(x => x.Description).Subscribe(x => _descriptionElement.Value = x);
            _descriptionElement.ValueChanged += (sender, e) => ViewModel.Description = _descriptionElement.Value;

            this.WhenViewModel(x => x.SubmitCommand).Subscribe(x =>
            {
                NavigationItem.RightBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Save, (s, e) =>
                {
                    ResignFirstResponder();
                    x.ExecuteIfCan();
                });

                x.CanExecuteObservable.Subscribe(y => NavigationItem.RightBarButtonItem.Enabled = y);
            });
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var source = new DialogTableViewSource(TableView, true);
            source.Root.Add(new Section { _titleElement, _descriptionElement });
            TableView.Source = source;
            TableView.TableFooterView = new UIView();
        }
    }
}
