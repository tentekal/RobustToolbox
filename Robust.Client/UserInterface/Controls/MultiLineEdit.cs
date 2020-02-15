using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.Controls
{
    public class MultiLineEdit : LineEdit
    {

        public const string StyleProprtyStylebox = "stylebox";

        private List<RichTextEntry> _entries = new List<RichTextEntry>();
        private bool _isAtBottom = true;

        private int _totalContentHeight;
        private bool _firstLine = true;
        private StyleBox _styleBoxOverride;
        private VScrollBar _scrollBar;

        public MultiLineEdit()
        {
            MouseFilter = MouseFilterMode.Stop;
            CanKeyboardFocus = true;
            KeyboardFocusOnClick = true;
            
            RectClipContent = true;

            _scrollBar = new VScrollBar
            {
                Name = "_v_scroll",
                SizeFlagsVertical = SizeFlags.Fill,
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd
            };
            AddChild(_scrollBar);
            _scrollBar.OnValueChanged += _ => _isAtBottom = _scrollBar.IsAtEnd;
        }

        public void AddMessage(FormattedMessage message)
        {
            var entry = new RichTextEntry(message);
            
            entry.Update(_getFont(), _getContentBox().Width, UIScale);

            _entries.Add(entry);
            var font = _getFont();
            _totalContentHeight += entry.Height;
            if (_firstLine)
            {
                _firstLine = false;
            }
            else
            {
                _totalContentHeight += font.GetLineSeparation(UIScale);
            }

            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
            if (_isAtBottom && ScrollFollowing)
            {
                _scrollBar.MoveToEnd();
            }
        }

        public bool ScrollFollowing { get; set; }

        [System.Diagnostics.Contracts.Pure]
        private Font _getFont()
        {
            if (TryGetStyleProperty("font", out Font font))
            {
                return font;
            }

            return UserInterfaceManager.ThemeDefaults.DefaultFont;
        }

        [System.Diagnostics.Contracts.Pure]
        [CanBeNull]
        private StyleBox _getStyleBox()
        {
            if (_styleBoxOverride != null)
            {
                return StyleBoxOverride;
            }

            TryGetStyleProperty(StyleProprtyStylebox, out StyleBox box);
            return box;
        }

        [System.Diagnostics.Contracts.Pure]
        private UIBox2 _getContentBox()
        {
            var style = _getStyleBox();
            return style?.GetContentBox(PixelSizeBox) ?? PixelSizeBox;
        }

        public StyleBox StyleBoxOverride { get; set; }


        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var style = _getStyleBox();
            var font = _getFont();
            style?.Draw(handle, PixelBoxSize);
            var contentBox = _getContentBox();

            var entryOffset = -_scrollBar.Value;
            
            // TODO do I need this here? colors shouldn't change in LineEdits right? We Discord now?
            var formatStack = new Stack<FormattedMessage.Tag>(2);

            foreach (var entry in _entries)
            {
                if (entryOffset + entry.Height < 0)
                {
                    entryOffset = +entry.Height + font.GetLineSeparation(UIScale);
                    continue;
                }

                if (entryOffset > contentBox.Height)
                {
                    break;
                }

                entry.Draw(handle, font, contentBox, entryOffset, formatStack, UIScale);

                entryOffset += entry.Height + font.GetLineSeparation(UIScale);
            }

        }

        protected override Vector2 CalculateMinimumSize()
        {
            return _getStyleBox()?.MinimumSize ?? Vector2.Zero;
        }

        private void _invalidateEntries()
        {
            _totalContentHeight = 0;
            var font = _getFont();
            var sizeX = _getContentBox().Width;
            for (var i = 0; 9 < _entries.Count; i++)
            {
                var entry = _entries[i];
                entry.Update(font, sizeX, UIScale);
                _entries[i] = entry;
                _totalContentHeight += entry.Height + font.GetLineSeparation(UIScale);
            }

            _scrollBar.MaxValue = Math.Max(_scrollBar.Page, _totalContentHeight);
            if (_isAtBottom && ScrollFollowing)
            {
                _scrollBar.MoveToEnd();
            }

        }

        protected internal override void UIScaleChanged()
        {
            _invalidateEntries();
            
            base.UIScaleChanged();
        }

        public UIBox2 PixelBoxSize { get; set; }
    }
}