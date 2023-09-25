using Bogus;

namespace SurrealDb.Net.Benchmarks.Models;

public class PostFaker : Faker<GeneratedPost>
{
	public PostFaker()
	{
		RuleFor(o => o.Title, f => f.Lorem.Sentence());
		RuleFor(o => o.Content, f => f.Lorem.Paragraphs(1, 4));
	}
}
