using UnityEngine;
using UnityEngine.Tilemaps;


[CreateAssetMenu(fileName = "FixedBoxBrush", menuName = "Brushes/Fixed Box Brush")]
public class FixedBoxBrush : UnityEditor.Tilemaps.GridBrush
{
    [Header("Cell Size (world units)")]
    public int cellSize = 10; // tama�o de cada celda en unidades de la escena

    private Vector3Int lastPaintedCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

    public override void Paint(GridLayout grid, GameObject brushTarget, Vector3Int position)
    {
        if (brushTarget == null) return;

        Tilemap tilemap = brushTarget.GetComponent<Tilemap>();
        if (tilemap == null) return;

        // Ajusta la posici�n a la cuadr�cula de 10x10
        int x = Mathf.FloorToInt(position.x / (float)cellSize) * cellSize;
        int y = Mathf.FloorToInt(position.y / (float)cellSize) * cellSize;
        Vector3Int alignedPosition = new Vector3Int(x, y, position.z);

        // Solo pintar si es una celda distinta a la anterior
        if (alignedPosition == lastPaintedCell) return;

        lastPaintedCell = alignedPosition;

        base.Paint(grid, brushTarget, alignedPosition);
    }
}