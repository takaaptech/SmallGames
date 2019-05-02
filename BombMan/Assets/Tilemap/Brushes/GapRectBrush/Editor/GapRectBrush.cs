using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

namespace UnityEditor {
    [CustomGridBrush(true, false, false, "GapRectBrush")]
    [CreateAssetMenu(fileName = "New GapRectBrush Brush", menuName = "Brushes/GapRectBrush Brush")]
    public class GapRectBrush : GridBrush {
        public int gapSize = 0;
        public bool lineStartActive = false;
        public bool fillGaps = false;
        public Vector3Int lineStart = Vector3Int.zero;

        public override void Paint(GridLayout grid, GameObject brushTarget, Vector3Int position){
            if (lineStartActive) {
                Vector2Int startPos = new Vector2Int(lineStart.x, lineStart.y);
                Vector2Int endPos = new Vector2Int(position.x, position.y);
                if (startPos == endPos)
                    base.Paint(grid, brushTarget, position);
                else {
                    foreach (var point in GetPointsInRect(startPos, endPos, gapSize)) {
                        Vector3Int paintPos = new Vector3Int(point.x, point.y, position.z);
                        base.Paint(grid, brushTarget, paintPos);
                    }
                }

                lineStartActive = false;
            }
            else {
                lineStart = position;
                lineStartActive = true;
            }
        }


        private static void Swap<T>(ref T a, ref T b){
            T temp = a;
            a = b;
            b = temp;
        }

        // http://ericw.ca/notes/bresenhams-line-algorithm-in-csharp.html
        public static IEnumerable<Vector2Int> GetPointsInRect(Vector2Int p1, Vector2Int p2, int gap){
            int x0 = p1.x;
            int y0 = p1.y;
            int x1 = p2.x;
            int y1 = p2.y;
            if (x0 > x1) {
                Swap(ref x0, ref x1);
            }
            if (y0 > y1) {
                Swap(ref y0, ref y1);
            }
            for (int x = x0; x <= x1; x += gap + 1) {
                for (int y = y0; y <= y1; y += gap + 1) {
                    yield return new Vector2Int(x, y);
                }
            }

            yield break;
        }
    }

    [CustomEditor(typeof(GapRectBrush))]
    public class GapRectBrushEditor : GridBrushEditor {
        private GapRectBrush lineBrush {
            get { return target as GapRectBrush; }
        }

        private Tilemap lastTilemap;

        public override void OnPaintSceneGUI(GridLayout grid, GameObject brushTarget, BoundsInt position,
            GridBrushBase.Tool tool, bool executing){
            base.OnPaintSceneGUI(grid, brushTarget, position, tool, executing);
            if (lineBrush.lineStartActive) {
                Tilemap tilemap = brushTarget.GetComponent<Tilemap>();
                if (tilemap != null)
                    lastTilemap = tilemap;

                // Draw preview tiles for tilemap
                Vector2Int startPos = new Vector2Int(lineBrush.lineStart.x, lineBrush.lineStart.y);
                Vector2Int endPos = new Vector2Int(position.x, position.y);
                if (startPos == endPos)
                    PaintPreview(grid, brushTarget, position.min);
                else {
                    foreach (var point in GapRectBrush.GetPointsInRect(startPos, endPos, lineBrush.gapSize)) {
                        Vector3Int paintPos = new Vector3Int(point.x, point.y, position.z);
                        PaintPreview(grid, brushTarget, paintPos);
                    }
                }

                if (Event.current.type == EventType.Repaint) {
                    var min = lineBrush.lineStart;
                    var max = lineBrush.lineStart + position.size;

                    // Draws a box on the picked starting position
                    GL.PushMatrix();
                    GL.MultMatrix(GUI.matrix);
                    GL.Begin(GL.LINES);
                    Handles.color = Color.blue;
                    Handles.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z));
                    Handles.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z));
                    Handles.DrawLine(new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z));
                    Handles.DrawLine(new Vector3(min.x, max.y, min.z), new Vector3(min.x, min.y, min.z));
                    GL.End();
                    GL.PopMatrix();
                }
            }
        }

        public override void ClearPreview(){
            base.ClearPreview();
            if (lastTilemap != null) {
                lastTilemap.ClearAllEditorPreviewTiles();
                lastTilemap = null;
            }
        }
    }
}