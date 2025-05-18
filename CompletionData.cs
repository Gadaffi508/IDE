using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Windows.Media;

namespace IDE
{
    public class CompletionData : ICompletionData
    {
        public CompletionData(string text)
        {
            Text = text;
        }

        public ImageSource Image => null;

        public string Text { get; private set; }

        public object Content => Text;

        public object Description => $"C# anahtar kelimesi: {Text}";

        public double Priority => 0;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            var document = textArea.Document;
            int caretOffset = textArea.Caret.Offset;

            int startOffset = caretOffset;
            while (startOffset > 0 && char.IsLetter(document.GetCharAt(startOffset - 1)))
                startOffset--;

            int length = caretOffset - startOffset;

            document.Replace(startOffset, length, Text);
        }


    }
}
