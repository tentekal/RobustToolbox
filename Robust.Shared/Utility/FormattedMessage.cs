using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Utility
{
    /// <summary>
    ///     Represents a formatted message in the form of a list of "tags".
    ///     Does not do any concrete formatting, simply useful as an API surface.
    /// </summary>
    [PublicAPI]
    [Serializable, NetSerializable]
    public sealed partial class FormattedMessage
    {
        public TagList Tags => new TagList(_tags);
        private readonly List<Tag> _tags;

        public FormattedMessage()
        {
            _tags = new List<Tag>();
        }

        public FormattedMessage(int capacity)
        {
            _tags = new List<Tag>(capacity);
        }

        public static FormattedMessage FromMarkup(string markup)
        {
            var msg = new FormattedMessage();
            msg.AddMarkup(markup);
            return msg;
        }

        /// <summary>
        ///     Escape a string of text to be able to be formatted into markup.
        /// </summary>
        public static string EscapeText(string text)
        {
            return text.Replace("\\", "\\\\").Replace("[", "\\[");
        }

        /// <summary>
        ///     Create a new <c>FormattedMessage</c> by copying another one.
        /// </summary>
        /// <param name="toCopy">The message to copy.</param>
        public FormattedMessage(FormattedMessage toCopy)
        {
            _tags = toCopy._tags.ShallowClone();
        }

        public void AddText(string text)
        {
            _tags.Add(new TagText(text));
        }

        public void PushColor(Color color)
        {
            _tags.Add(new TagColor(color));
        }

        public void Pop()
        {
            _tags.Add(new TagPop());
        }

        public void AddMessage(FormattedMessage other)
        {
            _tags.AddRange(other.Tags);
        }

        public void Clear()
        {
            _tags.Clear();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var tag in _tags)
            {
                if (!(tag is TagText text))
                {
                    continue;
                }

                builder.Append(text.Text);
            }

            return builder.ToString();
        }

        [Serializable, NetSerializable]
        public abstract class Tag
        {
        }

        [Serializable, NetSerializable]
        public class TagText : Tag
        {
            public readonly string Text;

            public TagText(string text)
            {
                Text = text;
            }
        }

        [Serializable, NetSerializable]
        public class TagColor : Tag
        {
            public readonly Color Color;

            public TagColor(Color color)
            {
                Color = color;
            }
        }

        [Serializable, NetSerializable]
        public class TagPop : Tag
        {
            public static readonly TagPop Instance = new TagPop();
        }

        public readonly struct TagList : IReadOnlyList<Tag>
        {
            private readonly List<Tag> _tags;

            public TagList(List<Tag> tags)
            {
                _tags = tags;
            }

            public List<Tag>.Enumerator GetEnumerator()
            {
                return _tags.GetEnumerator();
            }

            IEnumerator<Tag> IEnumerable<Tag>.GetEnumerator()
            {
                return _tags.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _tags.GetEnumerator();
            }

            public int Count => _tags.Count;

            public Tag this[int index] => _tags[index];
        }
    }
}
