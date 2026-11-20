using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WorldQuizRace.Application;
using WorldQuizRace.Application.Answers;
using WorldQuizRace.Domain.Enums;
using WorldQuizRace.Infrastructure.Seed;
using WorldQuizRace.Tests.Support;

namespace WorldQuizRace.Tests.Application;

/// <summary>Pure unit tests of the answer validators (docs/CHALLENGE_FORMATS.md).</summary>
public class AnswerValidatorTests
{
    private static readonly AnswerValidationService Service = CreateService();

    private static AnswerValidationService CreateService()
    {
        // Same discovery as production: every IAnswerValidator of the Application assembly.
        var validators = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IAnswerValidator).IsAssignableFrom(t))
            .Select(t => (IAnswerValidator)Activator.CreateInstance(t)!);
        return new AnswerValidationService(validators, NullLogger<AnswerValidationService>.Instance);
    }

    private static bool Check(ChallengeType type, string content, string correct, string submitted) =>
        Service.Validate(type, content, correct, AnswerFactory.Parse(submitted));

    // Content, expected answer, submission, expected verdict.
    public static TheoryData<ChallengeType, string, string, string, bool> Cases => new()
    {
        // MultipleChoice
        { ChallengeType.MultipleChoice, """{"prompt":"p","options":[{"id":"a","text":"A"},{"id":"b","text":"B"}]}""", """{"optionId":"b"}""", """{"optionId":"b"}""", true },
        { ChallengeType.MultipleChoice, """{"prompt":"p","options":[{"id":"a","text":"A"},{"id":"b","text":"B"}]}""", """{"optionId":"b"}""", """{"optionId":"a"}""", false },
        { ChallengeType.MultipleChoice, """{"prompt":"p","options":[]}""", """{"optionId":"b"}""", """{"optionId":["b"]}""", false },
        // TrueFalse
        { ChallengeType.TrueFalse, """{"prompt":"p"}""", """{"value":false}""", """{"value":false}""", true },
        { ChallengeType.TrueFalse, """{"prompt":"p"}""", """{"value":false}""", """{"value":"false"}""", true },
        { ChallengeType.TrueFalse, """{"prompt":"p"}""", """{"value":false}""", """{"value":true}""", false },
        { ChallengeType.TrueFalse, """{"prompt":"p"}""", """{"value":false}""", """{"value":0}""", false },
        // TextAnswer: case, accents, extra spaces and final punctuation ignored
        { ChallengeType.TextAnswer, """{"prompt":"p"}""", """{"acceptedAnswers":["la loire","loire"]}""", """{"text":"  La   LOIRE ! "}""", true },
        { ChallengeType.TextAnswer, """{"prompt":"p"}""", """{"acceptedAnswers":["toutankhamon"]}""", """{"text":"Toutânkhamon"}""", true },
        { ChallengeType.TextAnswer, """{"prompt":"p"}""", """{"acceptedAnswers":["loire"]}""", """{"text":"Seine"}""", false },
        { ChallengeType.TextAnswer, """{"prompt":"p"}""", """{"acceptedAnswers":["loire"]}""", """{"text":""}""", false },
        { ChallengeType.TextAnswer, """{"prompt":"p"}""", """{"acceptedAnswers":["loire"]}""", """{"text":{"value":"loire"}}""", false },
        // NumericAnswer: "3,5" = "3.5", tolerance
        { ChallengeType.NumericAnswer, """{"prompt":"p"}""", """{"value":3.5,"tolerance":0}""", """{"value":"3,5"}""", true },
        { ChallengeType.NumericAnswer, """{"prompt":"p"}""", """{"value":3.5,"tolerance":0}""", """{"value":3.5}""", true },
        { ChallengeType.NumericAnswer, """{"prompt":"p"}""", """{"value":1000,"tolerance":0}""", """{"value":"1 000"}""", true },
        { ChallengeType.NumericAnswer, """{"prompt":"p"}""", """{"value":10,"tolerance":0.5}""", """{"value":10.4}""", true },
        { ChallengeType.NumericAnswer, """{"prompt":"p"}""", """{"value":3,"tolerance":0}""", """{"value":4}""", false },
        { ChallengeType.NumericAnswer, """{"prompt":"p"}""", """{"value":3,"tolerance":0}""", """{"value":"trois"}""", false },
        // DragAndDrop
        { ChallengeType.DragAndDrop, "{}", """{"mapping":{"i1":"t1","i2":"t2"}}""", """{"mapping":{"i2":"t2","i1":"t1"}}""", true },
        { ChallengeType.DragAndDrop, "{}", """{"mapping":{"i1":"t1","i2":"t2"}}""", """{"mapping":{"i1":"t2","i2":"t2"}}""", false },
        { ChallengeType.DragAndDrop, "{}", """{"mapping":{"i1":"t1","i2":"t2"}}""", """{"mapping":{"i1":"t1"}}""", false },
        { ChallengeType.DragAndDrop, "{}", """{"mapping":{"i1":"t1"}}""", """{"mapping":["i1","t1"]}""", false },
        // ShapeSorting
        { ChallengeType.ShapeSorting, "{}", """{"mapping":{"s1":"b1","s2":"b2"}}""", """{"mapping":{"s1":"b1","s2":"b2"}}""", true },
        { ChallengeType.ShapeSorting, "{}", """{"mapping":{"s1":"b1","s2":"b2"}}""", """{"mapping":{"s1":"b2","s2":"b1"}}""", false },
        { ChallengeType.ShapeSorting, "{}", """{"mapping":{"s1":"b1"}}""", """{"bins":{"s1":"b1"}}""", false },
        // ImageMatching
        { ChallengeType.ImageMatching, "{}", """{"pairs":{"l1":"r2","l2":"r1"}}""", """{"pairs":{"l2":"r1","l1":"r2"}}""", true },
        { ChallengeType.ImageMatching, "{}", """{"pairs":{"l1":"r2","l2":"r1"}}""", """{"pairs":{"l1":"r1","l2":"r2"}}""", false },
        { ChallengeType.ImageMatching, "{}", """{"pairs":{"l1":"r2"}}""", """{"pairs":"l1-r2"}""", false },
        // MemoryCards: order ignored inside and between pairs
        { ChallengeType.MemoryCards, "{}", """{"pairs":[["c1","c3"],["c2","c4"]]}""", """{"pairs":[["c4","c2"],["c3","c1"]]}""", true },
        { ChallengeType.MemoryCards, "{}", """{"pairs":[["c1","c3"],["c2","c4"]]}""", """{"pairs":[["c1","c2"],["c3","c4"]]}""", false },
        { ChallengeType.MemoryCards, "{}", """{"pairs":[["c1","c3"],["c2","c4"]]}""", """{"pairs":[["c1","c3"],["c1","c3"]]}""", false },
        { ChallengeType.MemoryCards, "{}", """{"pairs":[["c1","c3"]]}""", """{"pairs":[["c1","c3","c5"]]}""", false },
        // SequenceCompletion: exact order, numbers compared numerically
        { ChallengeType.SequenceCompletion, "{}", """{"values":["11","17"]}""", """{"values":["11"," 17 "]}""", true },
        { ChallengeType.SequenceCompletion, "{}", """{"values":["I"]}""", """{"values":["i"]}""", true },
        { ChallengeType.SequenceCompletion, "{}", """{"values":["11","17"]}""", """{"values":["17","11"]}""", false },
        { ChallengeType.SequenceCompletion, "{}", """{"values":["11","17"]}""", """{"values":"11,17"}""", false },
        // LogicPuzzle: depends on answerType
        { ChallengeType.LogicPuzzle, """{"prompt":"p","answerType":"number"}""", """{"value":13,"tolerance":0}""", """{"value":"13"}""", true },
        { ChallengeType.LogicPuzzle, """{"prompt":"p","answerType":"choice","options":[{"id":"a","text":"A"}]}""", """{"optionId":"a"}""", """{"optionId":"b"}""", false },
        { ChallengeType.LogicPuzzle, """{"prompt":"p","answerType":"text"}""", """{"acceptedAnswers":["def"]}""", """{"text":"DEF"}""", true },
        { ChallengeType.LogicPuzzle, """{"prompt":"p","answerType":"number"}""", """{"value":13,"tolerance":0}""", """{"optionId":"13"}""", false },
        // Maze: adjacent cells, no wall, ends on E (any valid path)
        { ChallengeType.Maze, """{"prompt":"p","grid":["S.#.","#..#","#.#.","#..E"]}""", """{"shortestPathLength":6}""", """{"moves":"RDDDRR"}""", true },
        { ChallengeType.Maze, """{"prompt":"p","grid":["S.#.","#..#","#.#.","#..E"]}""", """{"shortestPathLength":6}""", """{"moves":["R","D","R","L","D","D","R","R"]}""", true },
        { ChallengeType.Maze, """{"prompt":"p","grid":["S.#.","#..#","#.#.","#..E"]}""", """{"shortestPathLength":6}""", """{"moves":"RRDDDR"}""", false },
        { ChallengeType.Maze, """{"prompt":"p","grid":["S.#.","#..#","#.#.","#..E"]}""", """{"shortestPathLength":6}""", """{"moves":"RDDD"}""", false },
        { ChallengeType.Maze, """{"prompt":"p","grid":["S.#.","#..#","#.#.","#..E"]}""", """{"shortestPathLength":6}""", """{"moves":"LDDDRR"}""", false },
        { ChallengeType.Maze, """{"prompt":"p","grid":["S.#.","#..#","#.#.","#..E"]}""", """{"shortestPathLength":6}""", """{"moves":"RDXDRR"}""", false },
        // OrderingClassification: order is exact, classify is a mapping
        { ChallengeType.OrderingClassification, """{"prompt":"p","mode":"order"}""", """{"order":["i2","i1","i3"]}""", """{"order":["i2","i1","i3"]}""", true },
        { ChallengeType.OrderingClassification, """{"prompt":"p","mode":"order"}""", """{"order":["i2","i1","i3"]}""", """{"order":["i1","i2","i3"]}""", false },
        { ChallengeType.OrderingClassification, """{"prompt":"p","mode":"classify"}""", """{"mapping":{"i1":"g1","i2":"g2"}}""", """{"mapping":{"i1":"g1","i2":"g2"}}""", true },
        { ChallengeType.OrderingClassification, """{"prompt":"p","mode":"order"}""", """{"order":["i2","i1"]}""", """{"order":{"0":"i2"}}""", false },
        // MathMiniGame: every result must be right
        { ChallengeType.MathMiniGame, "{}", """{"answers":{"p1":15,"p2":7}}""", """{"answers":{"p1":"15","p2":7}}""", true },
        { ChallengeType.MathMiniGame, "{}", """{"answers":{"p1":15,"p2":7}}""", """{"answers":{"p1":15,"p2":8}}""", false },
        { ChallengeType.MathMiniGame, "{}", """{"answers":{"p1":15,"p2":7}}""", """{"answers":{"p1":15}}""", false },
        { ChallengeType.MathMiniGame, "{}", """{"answers":{"p1":15}}""", """{"answers":[15]}""", false },
        // VisualChallenge: same set, any order
        { ChallengeType.VisualChallenge, "{}", """{"selectedIds":["v1","v3"]}""", """{"selectedIds":["v3","v1"]}""", true },
        { ChallengeType.VisualChallenge, "{}", """{"selectedIds":["v1","v3"]}""", """{"selectedIds":["v1"]}""", false },
        { ChallengeType.VisualChallenge, "{}", """{"selectedIds":["v1","v3"]}""", """{"selectedIds":["v1","v3","v5"]}""", false },
        { ChallengeType.VisualChallenge, "{}", """{"selectedIds":["v1"]}""", """{"selectedIds":"v1"}""", false },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validator_follows_the_challenge_format(ChallengeType type, string content, string correct, string submitted, bool expected)
    {
        Assert.Equal(expected, Check(type, content, correct, submitted));
    }

    [Fact]
    public void Every_challenge_type_has_exactly_one_validator_and_one_correct_and_incorrect_case()
    {
        Assert.Equal(Enum.GetValues<ChallengeType>().ToHashSet(), Service.SupportedTypes.ToHashSet());

        var cases = Cases.Select(row => ((ChallengeType)row[0], (bool)row[4])).ToList();
        Assert.All(Enum.GetValues<ChallengeType>(), type =>
        {
            Assert.Contains((type, true), cases);
            Assert.Contains((type, false), cases);
        });
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("\"optionId\"")]
    [InlineData("42")]
    [InlineData("null")]
    public void Non_object_answers_are_wrong_for_every_type(string submitted)
    {
        Assert.All(Enum.GetValues<ChallengeType>(), type =>
            Assert.False(Service.Validate(type, "{}", """{"optionId":"a"}""", AnswerFactory.Parse(submitted))));
    }

    [Fact]
    public void Undefined_answer_is_wrong_and_does_not_throw()
    {
        Assert.False(Service.Validate(ChallengeType.MultipleChoice, "{}", """{"optionId":"a"}""", default));
    }

    [Fact]
    public void Every_seeded_challenge_accepts_its_correct_answer_and_rejects_a_wrong_one()
    {
        var failures = new List<string>();
        foreach (var seed in SeedDataReader.ReadChallenges())
        {
            var content = seed.Content.GetRawText();
            var correct = seed.CorrectAnswer.GetRawText();
            if (!Service.Validate(seed.Type, content, correct, AnswerFactory.Correct(seed.Type, content, correct)))
            {
                failures.Add($"{seed.Code}: correct answer rejected");
            }

            if (Service.Validate(seed.Type, content, correct, AnswerFactory.Wrong))
            {
                failures.Add($"{seed.Code}: wrong answer accepted");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void Validators_are_registered_through_dependency_injection()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        services.AddApplication();
        using var provider = services.BuildServiceProvider();

        var validators = provider.GetServices<IAnswerValidator>().ToList();
        Assert.Equal(Enum.GetValues<ChallengeType>().Length, validators.Count);
        Assert.IsType<AnswerValidationService>(provider.GetRequiredService<IAnswerValidationService>());
    }
}
