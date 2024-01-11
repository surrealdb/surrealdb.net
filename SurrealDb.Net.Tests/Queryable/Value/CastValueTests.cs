// using SurrealDb.Net.Tests.Queryable.Models;
//
// namespace SurrealDb.Net.Tests.Queryable.Value;
//
// public class CastValueTests : BaseQueryableTests
// {
//     [Test]
//     public void EnumToInt()
//     {
//         string query = ToSurql(Posts.Select(p => (int)TestEnum.Alpha));
//
//         query
//             .Should()
//             .Be(
//                 """
//                 SELECT VALUE 1 FROM post
//                 """
//             );
//     }
//
//     [Test]
//     public void EnumToIntUsingParameter()
//     {
//         TestEnum value = TestEnum.Alpha;
//         string query = ToSurql(Posts.Select(p => (int)value));
//
//         query
//             .Should()
//             .Be(
//                 """
//                 SELECT VALUE 1 FROM post
//                 """
//             );
//     }
// }
