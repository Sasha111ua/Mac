using System;
using System.Collections.Generic;
using MonoMac.Foundation;
using MonoMac.AppKit;
using McViewModel;
using System.Drawing;
using Util.ResourceManagement;
using MonoMac.CoreGraphics;
using MonoMac.CoreAnimation;
using Util;
using McViewModel.SpecifiedTypes;

namespace McGui
{
    internal sealed partial class InfoSwitcherController : UController,IUpdatableView
    {
        #region Fields

        NNode _node;
        CAShapeLayer _backgrundLayer;
        NSOutlineViewDataSource _ds;

        #endregion

        #region Properties

        public Dictionary<IntPtr,NSView> ListOfView{ get; set; }

        public new InfoSwitcher View
        {
            get
            {
                return (InfoSwitcher)base.View;
            }
        }

        public NSOutlineViewDataSource ViewDataSource
        { 
            get
            { 
                return _ds;
            }
            set
            {
                var ds = value as TreeNodeOutlineDataSource; 
                if (ds != null)
                    _node = ds.GetRootNode();
                _ds = value;
            }
        }

        #endregion

        #region Events

        public event Action<NNode> SelectionChange;
        public event Action<bool> IsCheckedChange;

        #endregion

        #region Constructors

        // Called when created from unmanaged code
        public InfoSwitcherController(IntPtr handle) : base(handle)
        {
            throw new CannotInvokeConstructorException(handle, "InfoSwitcherController");
        }
        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public InfoSwitcherController(NSCoder coder) : base(coder)
        {
            throw new CannotInvokeConstructorException(coder, "InfoSwitcherController");
        }

        public InfoSwitcherController() : base("InfoSwitcher", NSBundle.MainBundle)
        {
        }

        #endregion

        #region Overrided Members

        public override void Reset()
        {
            if (ListOfView.Count > 0)
                ListOfView.Clear();

            if (_ds != null)
            { 
                _ds = null;
                _node = null;
                _backgrundLayer = null;
            }
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            _ol_sortButton.Create(SortedViewPopupString.Name | SortedViewPopupString.Size);
            _ol_infoSwitcherView.WantsLayer = true;
            _backgrundLayer = new  CAShapeLayer
            { 
                CornerRadius = InfoSwitcherFloats.CornerRadius.AsResourceFloat(), 
                BorderColor = InfoSwitcherColors.BorderColor.AsResourceCgColor(), 
                BackgroundColor = SortedViewColor.HeaderColor.AsResourceCgColor(), 
                BorderWidth = InfoSwitcherFloats.BorderWidth.AsResourceFloat()
            };
            SortOutline(_ol_sortButton.CurrentSortByTitle);
            _ol_infoSwitcherView.Layer = _backgrundLayer;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            _ol_infoSwitcherOutline.Delegate = new InfoSwitcherDeledage(this);
            _ol_infoSwitcherOutline.DataSource = ViewDataSource;
            _ol_infoSwitcherOutline.BackgroundColor = InfoSwitcherColors.BackgroundColorOutline.AsResourceNsColor();
            RaiseCheckChange();
            var frame = _ol_infoSwitcherView.Frame;
            var panelHeight = InfoSwitcherFloats.PanelHeight.AsResourceFloat();
            _ol_infoSwitcherView.SetFrameSize(new SizeF(frame.Width, _ol_infoSwitcherOutline.RowHeight * _node.ChildCount + panelHeight));
            var path = new CGPath();
            path.MoveToPoint(0, _ol_infoSwitcherView.Frame.Height - panelHeight);
            path.AddLineToPoint(_ol_infoSwitcherOutline.Frame.Width, _ol_infoSwitcherView.Frame.Height - panelHeight);
            _backgrundLayer.Path = path;
            _backgrundLayer.LineWidth = InfoSwitcherFloats.BorderWidth.AsResourceFloat();
            _backgrundLayer.StrokeColor = InfoSwitcherColors.BorderColor.AsResourceCgColor();
        }

        #endregion

        #region Methods

        partial void SortByButtonClick(NSObject sender)
        {
            var currPopup = sender as SortByPopupButton;
            if (currPopup != null)
                _ol_sortButton.CurrentSortByTitle = currPopup.SelectedItem.Title;
            if (_ol_sortButton.IsSortOutline)
                SortOutline(_ol_sortButton.CurrentSortByTitle);
        }

        void SortOutline(string sortType)
        {
            if (sortType == SortedViewPopupString.Size.AsResourceNsString() || sortType == SortedViewPopupString.Name.AsResourceNsString())
            { 
                var sort = sortType == SortedViewPopupString.Size.AsResourceNsString() ? Sortby.Size : Sortby.Name;
                if (_ol_sortButton.IsAscending)
                    _node.Sort(sort, true);
                else
                    _node.Sort(sort, false, true);
                if (_ol_infoSwitcherOutline.DataSource as TreeNodeOutlineDataSource == null)
                    _ol_infoSwitcherOutline.DataSource = new TreeNodeOutlineDataSource(_node, false);
                else
                    _ol_infoSwitcherOutline.ReloadData();
            }
            //            else if (sortType == SortedViewPopupString.Name.AsResourceNsString())
            //            {
            //                if (_sortButton.IsAscending)
            //                    _node.Sort(Sortby.Name, true);
            //                else
            //                    _node.Sort(Sortby.Name, false, true);
            //                if (_infoSwitcherOutline.DataSource as TreeNodeOutlineDataSource == null)
            //                    _infoSwitcherOutline.DataSource = new TreeNodeOutlineDataSource(_node);
            //                else
            //                    _infoSwitcherOutline.ReloadData();
            //            }
        }

        public void UpdateView()
        {
            if (_backgrundLayer == null || _ol_infoSwitcherOutline == null || _ol_sortButton == null)
                return;
            _ol_infoSwitcherOutline.ReloadData();

            var del = _ol_infoSwitcherOutline.Delegate as OutlineSortedViewDelegate;
            if (del != null)
                del.OnUpdate();
            if (_ol_sortButton.IsSortOutline)
                SortOutline(_ol_sortButton.CurrentSortByTitle);
        }

        public void RaiseSelectionChange()
        {
            var selectedItemId = _ol_infoSwitcherOutline.SelectedRow;
            var ds = _ol_infoSwitcherOutline.DataSource as TreeNodeOutlineDataSource;
            if (ds != null && selectedItemId >= 0)
            {
                var root = ds.GetRootNode();
                if (root.ChildCount > selectedItemId && SelectionChange != null)
                    SelectionChange(root.Children[selectedItemId]);
            }
        }

        public void CheckClick(NNode node, object sender)
        {
            var button = sender as NSButton;
            if (button != null)
            {
                node.IsChecked = button.State == NSCellStateValue.Mixed ? NSCellStateValue.On : button.State;
                RaiseCheckChange();
            }
        }

        void RaiseCheckChange()
        {  
            var ds = _ol_infoSwitcherOutline.DataSource as TreeNodeOutlineDataSource;
            if (IsCheckedChange != null)
            {
                if (ds != null)
                {
                    var root = ds.GetRootNode(); 
                    IsCheckedChange(root.State != false);
                }
                else
                    IsCheckedChange(false);
            }
            UpdateView();
        }

        #endregion
    }

    [Register("InfoSwitcherOutline")]
    internal sealed class InfoSwitcherOutline : SelectionHighlightOutline
    {
        #region Constructors

        public InfoSwitcherOutline(IntPtr handle) : base(handle)
        {
            throw new CannotInvokeConstructorException(handle, "InfoSwitcherOutline");
        }

        [Export("initWithCoder:")]
        public InfoSwitcherOutline(NSCoder coder) : base(coder)
        {

        }

        #endregion

        public override void MouseExited(NSEvent theEvent)
        {
            base.MouseExited(theEvent);
            if (RowView != null)
            {
                RowView.BackgroundColor = NSColor.Clear;
                RowView = null;
            }
        }
    }

    internal sealed class InfoSwitcherDeledage : OutlineSortedViewDelegate
    {
        #region Fields

        ArrowButton _arrawButton;
        readonly IUpdatableView _app;
        readonly List<NSButton> _listOfButton;
        readonly Dictionary<IntPtr, ColoredTableRowView> _cashedRowViews;

        #endregion

        #region Constructors

        public InfoSwitcherDeledage(IUpdatableView app) : base(app)
        {
            _listOfButton = new List<NSButton>();
            _app = app;
            _cashedRowViews = new Dictionary<IntPtr, ColoredTableRowView>();
           
        }

        public InfoSwitcherDeledage(IntPtr handle) : base(handle)
        {
            throw new CannotInvokeConstructorException(handle, "InfoSwitcherDeledage");
        }

        #endregion

        #region Overrided

        public override NSTableRowView RowViewForItem(NSOutlineView outlineView, NSObject item)
        {
            var view = new ColoredTableRowView(SortedOutlineIntegers.DisableRowSelectionGradient.AsResourceInterger() == 1, false);
            ColoredTableRowView cachedRowView;
            if (_cashedRowViews.TryGetValue(item.Handle, out cachedRowView))
                return cachedRowView;
            if (outlineView.RowForItem(item) != outlineView.RowCount - 1)
                view.CreateHorizontalSeparator(0, SortedViewImages.SortedViewSeparator.AsResourceNsImage(), RowHeight, MainViewFloats.SeparatorsImageHeight.AsFloat());
            _cashedRowViews.Add(item.Handle, view);
            return view;
        }

        protected override LabelControl CreateLabelDescription(NNode node, NSOutlineView outlineView, NSView view, NSTableColumn tableColumn)
        {
            var label = new LabelControl(new RectangleF(InfoSwitcherFloats.NameLabelX.AsResourceFloat(), RowHeight / 2 - InfoSwitcherFloats.NameLabelY.AsResourceFloat(), tableColumn.Width - InfoSwitcherFloats.NameLabelX.AsResourceFloat(), InfoSwitcherFloats.NameLabelY.AsResourceFloat()))
            {
                StringValue = node.ItemDescription,  
                TextColor = InfoSwitcherColors.AdditionalText.AsResourceNsColor(),
                Font = InfoSwitcherFonts.AdditionalText.AsResourceNsFont()
            };
            label.Cell.LineBreakMode = NSLineBreakMode.TruncatingTail;
            view.AddSubview(new LabelControl(new RectangleF(InfoSwitcherFloats.NameLabelX.AsResourceFloat(), RowHeight / 2 - InfoSwitcherFloats.AdditionalTextY.AsResourceFloat(), tableColumn.Width - InfoSwitcherFloats.NameLabelX.AsResourceFloat(), InfoSwitcherFloats.NameLabelY.AsResourceFloat()))
            { 
                StringValue = node.Name,
                ToolTip = node.ItemDescription,
                Font = InfoSwitcherFonts.NameTextFont.AsResourceNsFont()
            });
            return label;
        }

        public override float GetRowHeight(NSOutlineView outlineView, NSObject item)
        {
            return RowHeight = outlineView.RowHeight;
        }

        public override NSView ViewForTableColumn(NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
        {
            var viewWithArrow = base.ViewForTableColumn(outlineView, tableColumn, item);
            var node = item as NNode;
            if (node != null)
            {
                if (tableColumn.Identifier == PARAMs.NameClmnIdentifier)
                {  
                    var view = new NSView(new RectangleF(0, RowHeight / 2 - InfoSwitcherFloats.ViewY.AsResourceFloat(), InfoSwitcherFloats.ViewWidth.AsResourceFloat(), InfoSwitcherFloats.ViewHeight.AsResourceFloat()));
                    var label = CreateLabelDescription(node, outlineView, view, tableColumn);
                    NSButton btn = null;
                    foreach (var currHandle in _app.ListOfView.Keys)
                    {
                        if (currHandle == item.Handle)
                            btn = _app.ListOfView[currHandle] as NSButton;
                    }
                    if (btn == null)
                    {
                        btn = new NSButton(new RectangleF(InfoSwitcherFloats.CheckBoxX.AsResourceFloat(), RowHeight / 2 - InfoSwitcherFloats.CheckBoxY.AsResourceFloat(), InfoSwitcherFloats.CheckBoxWidthHeight.AsResourceFloat(), InfoSwitcherFloats.CheckBoxWidthHeight.AsResourceFloat())){ BezelStyle = NSBezelStyle.RegularSquare, AutoresizingMask = NSViewResizingMask.NotSizable };
                        var buttonCell = new NSButtonCell{ BezelStyle = NSBezelStyle.RegularSquare, Title = "", AllowsMixedState = true };
                        buttonCell.SetButtonType(NSButtonType.Switch);
                        btn.Cell = buttonCell;

                        if (node.State == null)
                            btn.State = NSCellStateValue.Mixed;
                        else
                            btn.State = node.State == false ? NSCellStateValue.Off : NSCellStateValue.On;

                        btn.Enabled = !node.IsEmpty;

                        btn.Activated += (sender, e) => _app.CheckClick(node, sender); 
                        _listOfButton.Add(btn);
                    }
                    view.AddSubview(label);
                    if (!node.IsCheckDisabled)
                        view.AddSubview(btn);
                    view.AddSubview(new NSImageView(new RectangleF(InfoSwitcherFloats.IconsX.AsResourceFloat(), InfoSwitcherFloats.IconsY.AsResourceFloat(), InfoSwitcherFloats.IconsWidthHeight.AsResourceFloat(), InfoSwitcherFloats.IconsWidthHeight.AsResourceFloat())){ Image = node.Image, AutoresizingMask = NSViewResizingMask.NotSizable });
                    return view;

                }
                if (tableColumn.Identifier == PARAMs.SizeClmnIdentifier)
                {
                    if (node.RealSize >= 0)
                    {
                        viewWithArrow.ClearView();
                        var par = new NSMutableParagraphStyle();
                        par.Alignment = NSTextAlignment.Right;
                        var attr = node.GetAttributedSize(InfoSwitcherFonts.SizeText.AsResourceNsFont(),
                                       InfoSwitcherColors.SizeText.AsResourceNsColor(),
                                       InfoSwitcherFonts.SizeTextMb.AsResourceNsFont(),
                                       InfoSwitcherColors.SizeText.AsResourceNsColor());
                        attr.AddAttribute(NSAttributedString.ParagraphStyleAttributeName, par, new NSRange(0, node.Size.Length - 1));
                        viewWithArrow.AddSubview(new LabelControl(new RectangleF(tableColumn.Width - InfoSwitcherFloats.SizeLabelX.AsResourceFloat(), RowHeight / 2 - InfoSwitcherFloats.SizeLabelY.AsResourceFloat(), tableColumn.Width - InfoSwitcherFloats.SizeWidth.AsResourceFloat(), InfoSwitcherFloats.SizeLabelHeight.AsResourceFloat()))
                        { 
                            Alignment = NSTextAlignment.Right, 
                            AttributedStringValue = attr
                        });
                    }
                    else
                    { 
                        var checkMark = new CheckMarkControl(SortedViewColor.CheckMarkColor.AsResourceCgColor());
                        checkMark.Frame = new RectangleF(tableColumn.Width - 40, RowHeight / 2 - 20, tableColumn.Width, 40);
                        viewWithArrow.AddSubview(checkMark);
                    }

                    var rectangle = new RectangleF(tableColumn.Width - InfoSwitcherFloats.ArrowButtonX.AsResourceFloat(), RowHeight / 2 - InfoSwitcherFloats.ArrowButtonY.AsResourceFloat(), InfoSwitcherFloats.ArrowButtonWidthHeight.AsResourceFloat(), InfoSwitcherFloats.ArrowButtonWidthHeight.AsResourceFloat());
                    _arrawButton = new ArrowButton(rectangle) { Bordered = false, Title = "", Image = InfoSwitcherImages.Arrow.AsResourceNsImage() };
                    _arrawButton.SetButtonType(NSButtonType.MomentaryChange);
                    _arrawButton.ToolTip = InfoSwitcherStrings.ArrowTooltip.AsResourceString();
                    viewWithArrow.AddSubview(_arrawButton);
                }
            }
            return viewWithArrow;
        }

        #endregion
    }
}


