using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Majinfwork.StateGraph {

    public class StateEdgeControl : EdgeControl {
        static readonly FieldInfo s_ControlPoints;
        static readonly FieldInfo s_RenderPoints;

        static StateEdgeControl() {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            s_ControlPoints = typeof(EdgeControl).GetField("m_ControlPoints", flags);
            s_RenderPoints = typeof(EdgeControl).GetField("m_RenderPoints", flags);
        }

        private static StateEdgeSettings Settings => StateEdgeSettings.Instance;

        protected override void ComputeControlPoints() {
            if (s_ControlPoints == null) {
                base.ComputeControlPoints();
                return;
            }

            Vector2 start = from;
            Vector2 end = to;
            Vector2 delta = end - start;
            float dist = delta.magnitude;

            if (dist < 1f) {
                base.ComputeControlPoints();
                return;
            }

            var s = Settings;
            bool isBackwards = delta.x < -20f;

            var points = new Vector2[4];
            points[0] = start;
            points[3] = end;

            if (isBackwards) {
                // Set control points that encompass the full backward route bounds
                // so the EdgeControl's layout rect covers the entire path
                float absDeltaX = Mathf.Abs(delta.x);
                float absDeltaY = Mathf.Abs(delta.y);
                float margin = Mathf.Max(s.margin, absDeltaX * s.marginDistanceScale);

                float yDir = delta.y >= 0 ? 1f : -1f;
                float midY;
                if (absDeltaY < s.cornerRadius * 4f) {
                    midY = start.y + yDir * Mathf.Max(s.minVerticalOffset, margin);
                    if (absDeltaY < 10f)
                        midY = start.y + Mathf.Max(s.minVerticalOffset, margin);
                }
                else {
                    midY = (start.y + end.y) * 0.5f;
                }

                // P1 = rightmost extent (right of start + margin)
                // P2 = leftmost extent (left of end - margin) at midY
                points[1] = new Vector2(start.x + margin, midY);
                points[2] = new Vector2(end.x - margin, midY);
            }
            else {
                float tangent = Mathf.Clamp(dist * s.forwardTangentRatio, s.forwardTangentMin, s.forwardTangentMax);
                points[1] = start + Vector2.right * tangent;
                points[2] = end + Vector2.left * tangent;
            }

            s_ControlPoints.SetValue(this, points);
        }

        protected override void UpdateRenderPoints() {
            // Always call base to handle coordinate transforms and internal state
            base.UpdateRenderPoints();

            Vector2 start = from;
            Vector2 end = to;
            bool isBackwards = (end.x - start.x) < -20f;

            if (!isBackwards || s_RenderPoints == null) return;

            // Read the base's render points to find the local-space offset
            var basePoints = s_RenderPoints.GetValue(this) as List<Vector2>;
            if (basePoints == null || basePoints.Count < 2) return;

            // Base puts 'from' at basePoints[0] in local space
            // The difference tells us the coordinate transform
            Vector2 offset = basePoints[0] - start;

            var route = ComputeBackwardsRoute(start, end);

            // Transform route into EdgeControl's local space
            for (int i = 0; i < route.Count; i++)
                route[i] += offset;

            s_RenderPoints.SetValue(this, route);
        }

        private List<Vector2> ComputeBackwardsRoute(Vector2 start, Vector2 end) {
            var s = Settings;
            var points = new List<Vector2>(32);

            float deltaY = end.y - start.y;
            float absDeltaY = Mathf.Abs(deltaY);
            float absDeltaX = Mathf.Abs(end.x - start.x);

            float margin = Mathf.Max(s.margin, absDeltaX * s.marginDistanceScale);
            float radius = Mathf.Min(s.cornerRadius, absDeltaY * 0.25f, margin * 0.5f);

            float yDir = deltaY >= 0 ? 1f : -1f;

            // If nodes are at similar Y, push the route above or below
            float midY;
            if (absDeltaY < radius * 4f) {
                midY = start.y + yDir * Mathf.Max(s.minVerticalOffset, margin);
                if (absDeltaY < 10f) {
                    midY = start.y + Mathf.Max(s.minVerticalOffset, margin);
                }
            }
            else {
                midY = (start.y + end.y) * 0.5f;
            }

            float ax = start.x + margin;
            float cx = end.x - margin;

            Vector2 A = new Vector2(ax, start.y);
            Vector2 B = new Vector2(ax, midY);
            Vector2 C = new Vector2(cx, midY);
            Vector2 D = new Vector2(cx, end.y);

            float yDirAB = midY > start.y ? 1f : -1f;
            float yDirCD = end.y > midY ? 1f : -1f;

            points.Add(start);

            AddRoundedCorner(points, A,
                Vector2.right, new Vector2(0, yDirAB), radius, s.cornerSegments);

            AddRoundedCorner(points, B,
                new Vector2(0, yDirAB), Vector2.left, radius, s.cornerSegments);

            AddRoundedCorner(points, C,
                Vector2.left, new Vector2(0, yDirCD), radius, s.cornerSegments);

            AddRoundedCorner(points, D,
                new Vector2(0, yDirCD), Vector2.right, radius, s.cornerSegments);

            points.Add(end);

            return points;
        }

        private void AddRoundedCorner(List<Vector2> points,
            Vector2 corner, Vector2 inDir, Vector2 outDir, float radius, int segments) {

            if (radius < 1f) {
                points.Add(corner);
                return;
            }

            Vector2 arcStart = corner - inDir * radius;
            Vector2 arcEnd = corner + outDir * radius;
            Vector2 center = corner - inDir * radius + outDir * radius;

            float startAngle = Mathf.Atan2(arcStart.y - center.y, arcStart.x - center.x);
            float endAngle = Mathf.Atan2(arcEnd.y - center.y, arcEnd.x - center.x);

            float sweep = endAngle - startAngle;
            if (sweep > Mathf.PI) sweep -= 2f * Mathf.PI;
            if (sweep < -Mathf.PI) sweep += 2f * Mathf.PI;

            for (int i = 0; i <= segments; i++) {
                float t = (float)i / segments;
                float angle = startAngle + sweep * t;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }
    }

    public class StateEdge : Edge {
        protected override EdgeControl CreateEdgeControl() {
            return new StateEdgeControl();
        }
    }
}
