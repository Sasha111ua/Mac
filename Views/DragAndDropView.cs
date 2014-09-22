using System;
using MonoMac.Foundation;
using MonoMac.AppKit;
using McViewModel;
using System.Linq;
using System.Collections.Generic;
using McViewModel.SpecifiedTypes;

namespace McGui
{
    [Register("DragAndDropView")]
    internal sealed class DragAndDropView:NSImageView
    {
        NSDragOperation _dragOperation = NSDragOperation.None;
        List<string> _list;
        UView _firstPageController;

        [Export("initWithCoder:")]
        public DragAndDropView(NSCoder coder) : base(coder)
        {

        }

        public DragAndDropView(IntPtr handle) : base(handle)
        {
            throw new CannotInvokeConstructorException(handle, "DecoratedViewController");
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            _dragOperation = NSDragOperation.None;
            _list = new List<string>();
            RegisterForDraggedTypes(new string[]{ NSPasteboard.NSFilenamesType });
        }

        public override NSDragOperation DraggingEntered(NSDraggingInfo sender)
        {
            var pasteboard = sender.DraggingPasteboard;

            if (pasteboard.Types.Contains(NSPasteboard.NSFilenamesType))
            {
                var filenames = pasteboard.GetPropertyListForType(NSPasteboard.NSFilenamesType) as NSArray;
                if (filenames != null)
                {
                    _firstPageController = Superview as UView;
                    if (_firstPageController != null)
                    {
                        _list.Clear();
                        for (uint i = 0; i < filenames.Count; i++)
                        {
                            _list.Add((string)NSString.FromHandle(filenames.ValueAt(i)));
                        }
                        if (_firstPageController.Controller.CanDropFiles(_list))
                        {
                            _dragOperation = NSDragOperation.Copy;
                            return NSDragOperation.Copy;
                        }
                    }
                }
            }
            _dragOperation = NSDragOperation.None;
            return NSDragOperation.None;
        }

        public override bool PerformDragOperation(NSDraggingInfo sender)
        {
            NSApplication.SharedApplication.ActivateIgnoringOtherApps(true);
            Window.MakeKeyAndOrderFront(sender);
            NSApplication.SharedApplication.ActivateIgnoringOtherApps(false);
            if (_dragOperation == NSDragOperation.Copy)
            {
                _firstPageController.Controller.DropFiles(_list);
                return  true;
            }
            return false;
        }

        public override NSImage Image
        {
            get
            {
                return base.Image;
            }
            set
            {

                //SRG: Should the setter be empty???
                //Sasha: yes , we disable basic behavior
            }
        }
    }
}

