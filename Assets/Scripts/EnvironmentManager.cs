using UnityEngine;
using Util;

public class EnvironmentManager : MonoLocator<EnvironmentManager>
{
    [SerializeField] private Vector2 _botLeftCorner = new Vector2(-35, -15);
    [SerializeField] private Vector2 _topRightCorner = new Vector2(35, 15);

    public Vector2 BotLeftCorner => _botLeftCorner;
    public Vector2 TopRightCorner => _topRightCorner;

    private void OnDrawGizmos()
    {
        var topLeftCorner = new Vector2(_botLeftCorner.x, _topRightCorner.y);
        var botRightCorner = new Vector2(_topRightCorner.x, _botLeftCorner.y);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(_botLeftCorner, topLeftCorner);
        Gizmos.DrawLine(topLeftCorner, _topRightCorner);
        Gizmos.DrawLine(_topRightCorner, botRightCorner);
        Gizmos.DrawLine(botRightCorner, _botLeftCorner);
    }
}
