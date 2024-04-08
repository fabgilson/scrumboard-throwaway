namespace ScrumBoard.Models
{
    public class SortableEventArgs {

        ///<summary>target list key</summary>
        public int To { get; set; }
        ///<summary>previous list key</summary>
        public int From { get; set; }
        ///<summary>element's old index within old parent</summary>
        public int OldIndex { get; set; }
        ///<summary>element's new index within new parent</summary>
        public int NewIndex { get; set; }
        ///<summary>element's old index within old parent, only counting draggable elements</summary>
        public int OldDraggableIndex { get; set; }
        ///<summary>// element's new index within new parent, only counting draggable elements</summary>
        public int NewDraggableIndex { get; set; }
    }
}