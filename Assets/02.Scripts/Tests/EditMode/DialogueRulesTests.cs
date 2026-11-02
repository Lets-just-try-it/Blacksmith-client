using System;
using System.Collections.Generic;
using Blacksmith.Core.Dialogue;
using NUnit.Framework;

namespace Blacksmith.Tests
{
    public class DialogueRulesTests
    {
        private sealed class FakeStats : IDialogueStats
        {
            private readonly Dictionary<DialogueStat, int> _values = new Dictionary<DialogueStat, int>();

            public FakeStats With(DialogueStat stat, int value)
            {
                _values[stat] = value;
                return this;
            }

            public int Get(DialogueStat stat)
            {
                return _values[stat];
            }
        }

        private static DialogueCondition Condition(DialogueCompare compare, int value)
        {
            return new DialogueCondition { Stat = DialogueStat.FreeRosterSlots, Compare = compare, Value = value };
        }

        [TestCase(DialogueCompare.AtLeast, 1, 0, false)]
        [TestCase(DialogueCompare.AtLeast, 1, 1, true)]
        [TestCase(DialogueCompare.AtLeast, 1, 2, true)]
        [TestCase(DialogueCompare.AtMost, 1, 0, true)]
        [TestCase(DialogueCompare.AtMost, 1, 1, true)]
        [TestCase(DialogueCompare.AtMost, 1, 2, false)]
        [TestCase(DialogueCompare.Equal, 1, 1, true)]
        [TestCase(DialogueCompare.Equal, 1, 2, false)]
        [TestCase(DialogueCompare.NotEqual, 1, 1, false)]
        [TestCase(DialogueCompare.NotEqual, 1, 2, true)]
        public void IsMet_ComparesCurrentValueWithConditionValue(DialogueCompare compare, int value, int current, bool expected)
        {
            var stats = new FakeStats().With(DialogueStat.FreeRosterSlots, current);
            Assert.AreEqual(expected, Condition(compare, value).IsMet(stats));
        }

        [Test]
        public void AreMet_WithoutConditions_IsTrue()
        {
            Assert.IsTrue(DialogueCondition.AreMet(Array.Empty<DialogueCondition>(), new FakeStats()));
        }

        [Test]
        public void AreMet_RequiresEveryCondition()
        {
            var stats = new FakeStats().With(DialogueStat.FreeRosterSlots, 2);
            var conditions = new[] { Condition(DialogueCompare.AtLeast, 1), Condition(DialogueCompare.AtMost, 1) };
            Assert.IsFalse(DialogueCondition.AreMet(conditions, stats));
        }

        [Test]
        public void Cursor_StartsWithoutHighlight_AndCannotConfirm()
        {
            var cursor = new DialogueChoiceCursor(new[] { true, true });
            Assert.AreEqual(DialogueChoiceCursor.NoHighlight, cursor.Highlighted);
            Assert.IsFalse(cursor.CanConfirm);
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void Cursor_MoveWithoutHighlight_HighlightsFirst(int direction)
        {
            var cursor = new DialogueChoiceCursor(new[] { true, true, true });
            cursor.Move(direction);
            Assert.AreEqual(0, cursor.Highlighted);
        }

        [Test]
        public void Cursor_Move_StopsAtEnds()
        {
            var cursor = new DialogueChoiceCursor(new[] { true, true });
            cursor.Move(1);
            cursor.Move(1);
            cursor.Move(1);
            Assert.AreEqual(1, cursor.Highlighted);

            cursor.Move(-1);
            cursor.Move(-1);
            Assert.AreEqual(0, cursor.Highlighted);
        }

        [Test]
        public void Cursor_HighlightNumber_IgnoresMissingNumber()
        {
            var cursor = new DialogueChoiceCursor(new[] { true, true });
            cursor.HighlightNumber(2);
            cursor.HighlightNumber(3);
            Assert.AreEqual(1, cursor.Highlighted);
        }

        [Test]
        public void Cursor_DisabledChoice_CanBeHighlightedButNotConfirmed()
        {
            var cursor = new DialogueChoiceCursor(new[] { true, false });
            cursor.HighlightNumber(2);
            Assert.AreEqual(1, cursor.Highlighted);
            Assert.IsFalse(cursor.CanConfirm);
        }

        [Test]
        public void Cursor_RejectsMoreThanMaxChoices()
        {
            Assert.Throws<ArgumentException>(() => new DialogueChoiceCursor(new bool[DialogueChoiceCursor.MaxChoices + 1]));
        }
    }
}
