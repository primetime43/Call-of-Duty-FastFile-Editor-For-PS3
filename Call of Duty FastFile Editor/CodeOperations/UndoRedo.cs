namespace Call_of_Duty_FastFile_Editor.CodeOperations
{
    /* Currently doesn't work */
    public class UndoRedo
    {
        private Stack<string> _undoStack;
        private Stack<string> _redoStack;

        public event EventHandler UndoRedoStackChanged;

        public UndoRedo()
        {
            _undoStack = new Stack<string>();
            _redoStack = new Stack<string>();
        }

        public void TrackChange(string currentText)
        {
            // Push the current text to the undo stack
            _undoStack.Push(currentText);
            // Clear the redo stack whenever a new change is made
            _redoStack.Clear();
            OnUndoRedoStackChanged();
        }

        public string Undo(string currentText)
        {
            if (_undoStack.Count > 0)
            {
                // Push the current text to the redo stack
                _redoStack.Push(currentText);
                // Pop the text from the undo stack and return it
                string previousText = _undoStack.Pop();
                OnUndoRedoStackChanged();
                return previousText;
            }
            return currentText;
        }

        public string Redo(string currentText)
        {
            if (_redoStack.Count > 0)
            {
                // Push the current text to the undo stack
                _undoStack.Push(currentText);
                // Pop the text from the redo stack and return it
                string nextText = _redoStack.Pop();
                OnUndoRedoStackChanged();
                return nextText;
            }
            return currentText;
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        protected virtual void OnUndoRedoStackChanged()
        {
            UndoRedoStackChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}