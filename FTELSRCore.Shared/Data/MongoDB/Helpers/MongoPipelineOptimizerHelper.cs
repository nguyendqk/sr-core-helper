using MongoDB.Bson;

namespace FTELSRCore.Data.MongoDB.Helpers
{
    public class MongoPipelineOptimizerHelper
    {
        public static List<BsonDocument> Optimize(List<BsonDocument> pipeline)
        {
            if (pipeline == null || pipeline.Count == 0)
            {
                return pipeline;
            }

            var result = new List<BsonDocument>(pipeline.Count);

            foreach (var stage in pipeline)
            {
                var optimized = OptimizeStage(stage);

                if (optimized is not null)
                {
                    result.Add(optimized);
                }
            }
            return result;
        }

        #region +++++++++++++++++++++++++++++ Helper +++++++++++++++++++++++++++++

        // Trả về null nếu stage bị loại bỏ hoàn toàn.
        private static BsonDocument OptimizeStage(BsonDocument stage)
        {
            if (stage == null || stage.ElementCount == 0)
                return stage;

            var first = stage.GetElement(0);
            var name = first.Name;
            var value = first.Value;

            switch (name)
            {
                case "$skip":
                    return (value.IsNumeric && value.ToInt64() == 0) ? null : stage;

                case "$match":
                    if (!value.IsBsonDocument) return stage;
                    var match = value.AsBsonDocument;
                    if (match.ElementCount == 0) return null;
                    var unwrapped = TryUnwrapAnd(match);
                    return unwrapped != null
                        ? new BsonDocument("$match", unwrapped)
                        : stage;

                case "$addFields":
                case "$set":
                    if (value.IsBsonDocument && value.AsBsonDocument.ElementCount == 0)
                        return null;
                    return stage;

                case "$unset":
                    if (value.IsBsonArray && value.AsBsonArray.Count == 0) return null;
                    if (value.IsString && string.IsNullOrEmpty(value.AsString)) return null;
                    return stage;

                case "$facet":
                    return value.IsBsonDocument
                        ? OptimizeFacet(value.AsBsonDocument)
                        : stage;

                case "$lookup":
                case "$unionWith":
                    return value.IsBsonDocument
                        ? OptimizeNestedPipeline(name, value.AsBsonDocument)
                        : stage;

                default:
                    return stage;
            }
        }

        private static BsonDocument OptimizeFacet(BsonDocument facet)
        {
            var newFacet = new BsonDocument();
            foreach (var sub in facet)
            {
                if (sub.Value.IsBsonArray)
                {
                    var subStages = sub.Value.AsBsonArray
                        .Where(x => x.IsBsonDocument)
                        .Select(x => x.AsBsonDocument)
                        .ToList();
                    newFacet.Add(sub.Name, new BsonArray(Optimize(subStages)));
                }
                else
                {
                    newFacet.Add(sub.Name, sub.Value);
                }
            }
            return new BsonDocument("$facet", newFacet);
        }

        private static BsonDocument OptimizeNestedPipeline(string stageName, BsonDocument body)
        {
            if (!body.Contains("pipeline") || !body["pipeline"].IsBsonArray)
                return new BsonDocument(stageName, body);

            var subStages = body["pipeline"].AsBsonArray
                .Where(x => x.IsBsonDocument)
                .Select(x => x.AsBsonDocument)
                .ToList();

            var copy = (BsonDocument)body.DeepClone();
            copy["pipeline"] = new BsonArray(Optimize(subStages));

            return new BsonDocument(stageName, copy);
        }

        // Unwrap $and chỉ khi KHÔNG có field name trùng → đảm bảo tương đương ngữ nghĩa.
        private static BsonDocument TryUnwrapAnd(BsonDocument match)
        {
            if (!match.Contains("$and") || !match["$and"].IsBsonArray)
            {
                return null;
            }

            var andArray = match["$and"].AsBsonArray;

            // $and: [] tương đương "luôn đúng" → có thể bỏ
            if (andArray.Count == 0)
            {
                var copy = (BsonDocument)match.DeepClone();
                copy.Remove("$and");

                return copy.ElementCount == 0 ? null : copy;
            }

            var combined = new BsonDocument();

            foreach (var el in match)
            {
                if (el.Name == "$and")
                {
                    continue;
                }

                combined.Add(el);
            }

            foreach (var item in andArray)
            {
                if (!item.IsBsonDocument)
                {
                    return null;
                }

                foreach (var field in item.AsBsonDocument)
                {
                    // Trùng field name → không thể merge (ví dụ $and:[{a:{$gt:1}},{a:{$lt:9}}])
                    if (combined.Contains(field.Name))
                    {
                        return null;
                    }
                    combined.Add(field);
                }
            }

            return combined;
        }

        #endregion +++++++++++++++++++++++++++++ Helper +++++++++++++++++++++++++++++
    }
}