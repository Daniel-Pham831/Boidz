using System;
using System.Collections.Generic;
using UnityEngine;

// BOT-LEFT inclusive, TOP-RIGHT exclusive
public class BoidQuadTree
{
    private const int MAX_ITEMS = 4;

    public BoidQuadTree(Vector2 centerPosition, Vector2 size, BoidQuadTree parent = null)
    {
        CenterPosition = centerPosition;
        Size = size;
        LeftEdge = centerPosition.x - size.x / 2;
        RightEdge = centerPosition.x + size.x / 2;
        TopEdge = centerPosition.y + size.y / 2;
        BottomEdge = centerPosition.y - size.y / 2;

        Items = new Dictionary<int,Vector2>(MAX_ITEMS);
        Parent = parent;

        if (parent != null)
        {
            parent.IncreaseDepthBottomUp();
        }
    }

    public readonly Vector2 Size;
    public readonly Vector2 CenterPosition;
    public readonly float LeftEdge;
    public readonly float RightEdge;
    public readonly float TopEdge;
    public readonly float BottomEdge;
    public int Depth { get; private set; }

    public readonly Dictionary<int,Vector2> Items;

    public readonly BoidQuadTree Parent = null;

    public BoidQuadTree BottomLeft;
    public BoidQuadTree TopLeft;
    public BoidQuadTree BottomRight;
    public BoidQuadTree TopRight;

    public bool IsLeaf => IsAllSubTreeNull || IsAllSubTreeEmpty;

    private bool IsAllSubTreeNull => BottomLeft == null &&
                                     TopLeft == null &&
                                     BottomRight == null &&
                                     TopRight == null;

    private bool IsAllSubTreeEmpty => (BottomLeft == null || BottomLeft.Items.Count == 0) &&
                                      (TopLeft == null || TopLeft.Items.Count == 0) &&
                                      (BottomRight == null || BottomRight.Items.Count == 0) &&
                                      (TopRight == null || TopRight.Items.Count == 0);

    // we ALWAYS can insert down the tree
    public void IncreaseDepthBottomUp()
    {
        Depth++;
        Parent?.IncreaseDepthBottomUp();
    }

    public bool InsertDown(int boidIndex, Vector2 boidPosition)
    {
        if (!IsInTree(boidPosition)) return false;

        if (Items.Count < MAX_ITEMS)
        {
            Items.TryAdd(boidIndex, boidPosition);
            return true;
        }
        else
        {
            var correctTree = this.GetFirstChildTreeAtPosition(boidPosition, true);
            return correctTree.InsertDown(boidIndex,boidPosition);
        }
    }

    public void ClearAll()
    {
        Items.Clear();
        BottomLeft?.ClearAll();
        TopLeft?.ClearAll();
        BottomRight?.ClearAll();
        TopRight?.ClearAll();
    }

    public bool IsInTree(Vector2 position)
    {
        return position.x >= LeftEdge && position.x < RightEdge && position.y >= BottomEdge && position.y < TopEdge;
    }

    // for the sake of simplicity, we know that item WILL ALWAYS be in the tree
    // so no need to check if it's in the tree or not
    public void EnumerateThroughAllIndicesAroundCircle(Vector2 position, float radius, Action<int> callback)
    {
        EnumerateThroughAllItemsAroundCircle_Internal(position, radius, callback);
    }

    private void EnumerateThroughAllItemsAroundCircle_Internal(Vector2 position, float radius, Action<int> callback)
    {
        if (IsLeaf)
        {
            foreach (var itemInTree in Items)
            {
                var checkPosition = itemInTree.Value;

                // early check if it's in BoundingRect of the circle (position-radius)
                if (checkPosition.x < position.x - radius || checkPosition.x > position.x + radius ||
                    checkPosition.y < position.y - radius || checkPosition.y > position.y + radius)
                {
                    continue;
                }

                var distanceSquared = (checkPosition - position).sqrMagnitude;
                if (distanceSquared <= radius * radius)
                {
                    callback?.Invoke(itemInTree.Key);
                }
            }
        }
        else
        {
            BottomLeft?.EnumerateThroughAllItemsAroundCircle_Internal(position, radius, callback);
            TopLeft?.EnumerateThroughAllItemsAroundCircle_Internal(position, radius, callback);
            TopRight?.EnumerateThroughAllItemsAroundCircle_Internal(position, radius, callback);
            BottomRight?.EnumerateThroughAllItemsAroundCircle_Internal(position, radius, callback);
        }
    }

    public void OnGizmosDraw(int maxDepth)
    {
        if (IsAllSubTreeEmpty) return;
        var color = Color.red;
        color.a = Mathf.Lerp(0.1f, 0.4f, this.Depth / (float)maxDepth);
        Gizmos.color = color;
        Gizmos.DrawWireCube(CenterPosition, Size);
        Gizmos.DrawCube(CenterPosition, Size);

        BottomLeft?.OnGizmosDraw(maxDepth);
        TopLeft?.OnGizmosDraw(maxDepth);
        BottomRight?.OnGizmosDraw(maxDepth);
        TopRight?.OnGizmosDraw(maxDepth);
    }
}

public static class BoidQuadTreeExt
{
    public static BoidQuadTree GetFirstChildTreeAtPosition(this BoidQuadTree tree, Vector2 position,
        bool shouldCreate)
    {
        var isLeft = position.x < tree.CenterPosition.x;
        var isBot = position.y < tree.CenterPosition.y;

        if (isLeft)
        {
            if (isBot)
            {
                // Bottom Left
                if (shouldCreate)
                    tree.BottomLeft ??= new BoidQuadTree(
                        new Vector2(tree.LeftEdge + tree.Size.x / 4, tree.BottomEdge + tree.Size.y / 4),
                        tree.Size / 2, tree);

                return tree.BottomLeft;
            }
            else
            {
                // Top Left
                if (shouldCreate)
                    tree.TopLeft ??= new BoidQuadTree(
                        new Vector2(tree.LeftEdge + tree.Size.x / 4, tree.TopEdge - tree.Size.y / 4), tree.Size / 2,
                        tree);

                return tree.TopLeft;
            }
        }
        else
        {
            if (isBot)
            {
                // Bottom Right
                if (shouldCreate)
                    tree.BottomRight ??= new BoidQuadTree(
                        new Vector2(tree.RightEdge - tree.Size.x / 4, tree.BottomEdge + tree.Size.y / 4),
                        tree.Size / 2, tree);

                return tree.BottomRight;
            }
            else
            {
                // Top Right
                if (shouldCreate)
                    tree.TopRight ??= new BoidQuadTree(
                        new Vector2(tree.RightEdge - tree.Size.x / 4, tree.TopEdge - tree.Size.y / 4),
                        tree.Size / 2,
                        tree);

                return tree.TopRight;
            }
        }
    }
}
