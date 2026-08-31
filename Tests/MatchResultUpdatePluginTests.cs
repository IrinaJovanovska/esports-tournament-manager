using System;
using System.Linq;
using FakeXrmEasy;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;

namespace MatchResultUpdatePlugin.Tests
{
    public class MatchResultUpdatePluginTests
    {
        // =====================================================
        // TEST 1
        // Winner / Loser standings are updated
        // =====================================================

        [Fact]
        public void MatchResult_Should_Update_Tournament_Standings()
        {
            // =================================================
            // ARRANGE
            // =================================================

            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();

            Guid tournamentId = Guid.NewGuid();
            Guid teamAId = Guid.NewGuid();
            Guid teamBId = Guid.NewGuid();
            Guid matchId = Guid.NewGuid();

            // -------------------------------------------------
            // Tournament
            // -------------------------------------------------

            Entity tournament = new Entity("sol_tournament");
            tournament.Id = tournamentId;

            tournament["sol_totalmatches"] = 2;
            tournament["sol_completedmatches"] = 0;
            tournament["sol_status"] =
                new OptionSetValue(2);

            // -------------------------------------------------
            // Match
            // -------------------------------------------------

            Entity match = new Entity("sol_match");
            match.Id = matchId;

            match["sol_tournament"] =
                new EntityReference(
                    "sol_tournament",
                    tournamentId);

            match["sol_teama"] =
                new EntityReference(
                    "sol_team",
                    teamAId);

            match["sol_teamb"] =
                new EntityReference(
                    "sol_team",
                    teamBId);

            context.Initialize(new[]
            {
                tournament,
                match
            });

            // =================================================
            // PRE IMAGE
            // =================================================

            Entity preImage = new Entity("sol_match");
            preImage.Id = matchId;

            preImage["sol_tournament"] =
                new EntityReference(
                    "sol_tournament",
                    tournamentId);

            preImage["sol_teama"] =
                new EntityReference(
                    "sol_team",
                    teamAId);

            preImage["sol_teamb"] =
                new EntityReference(
                    "sol_team",
                    teamBId);

            // Match had no previous winner
            preImage["sol_winner"] = null;

            // =================================================
            // TARGET
            // =================================================

            Entity target = new Entity("sol_match");
            target.Id = matchId;

            // Team A wins
            target["sol_winner"] =
                new EntityReference(
                    "sol_team",
                    teamAId);

            // =================================================
            // ACT
            // =================================================

            var plugin =
                new MatchResultUpdatePlugin.Plugins.MatchResultUpdatePlugin();

            var pluginContext =
                context.GetDefaultPluginContext();

            pluginContext.MessageName = "Update";
            pluginContext.PrimaryEntityName = "sol_match";

            pluginContext.InputParameters["Target"] =
                target;

            pluginContext.PreEntityImages["PreImage"] =
                preImage;

            context.ExecutePluginWith(
                pluginContext,
                plugin);

            // =================================================
            // ASSERT - TOURNAMENT
            // =================================================

            Entity updatedTournament =
                service.Retrieve(
                    "sol_tournament",
                    tournamentId,
                    new ColumnSet(
                        "sol_totalmatches",
                        "sol_completedmatches",
                        "sol_status"));

            Assert.Equal(
                1,
                updatedTournament.GetAttributeValue<int>(
                    "sol_completedmatches"));

            // Tournament is not completed yet
            Assert.Equal(
                2,
                updatedTournament
                    .GetAttributeValue<OptionSetValue>(
                        "sol_status")
                    .Value);

            // =================================================
            // ASSERT - TEAM A
            // =================================================

            EntityCollection teamAStandings =
                service.RetrieveMultiple(
                    new QueryExpression(
                        "sol_tournamentstanding")
                    {
                        ColumnSet = new ColumnSet(
                            "sol_tournament",
                            "sol_team",
                            "sol_wins",
                            "sol_losses",
                            "sol_points"),

                        Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression(
                                    "sol_tournament",
                                    ConditionOperator.Equal,
                                    tournamentId),

                                new ConditionExpression(
                                    "sol_team",
                                    ConditionOperator.Equal,
                                    teamAId)
                            }
                        }
                    });

            Assert.Single(teamAStandings.Entities);

            Entity teamAStanding =
                teamAStandings.Entities[0];

            Assert.Equal(
                1,
                teamAStanding.GetAttributeValue<int>(
                    "sol_wins"));

            Assert.Equal(
                0,
                teamAStanding.GetAttributeValue<int>(
                    "sol_losses"));

            Assert.Equal(
                3,
                teamAStanding.GetAttributeValue<int>(
                    "sol_points"));

            // =================================================
            // ASSERT - TEAM B
            // =================================================

            EntityCollection teamBStandings =
                service.RetrieveMultiple(
                    new QueryExpression(
                        "sol_tournamentstanding")
                    {
                        ColumnSet = new ColumnSet(
                            "sol_tournament",
                            "sol_team",
                            "sol_wins",
                            "sol_losses",
                            "sol_points"),

                        Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression(
                                    "sol_tournament",
                                    ConditionOperator.Equal,
                                    tournamentId),

                                new ConditionExpression(
                                    "sol_team",
                                    ConditionOperator.Equal,
                                    teamBId)
                            }
                        }
                    });

            Assert.Single(teamBStandings.Entities);

            Entity teamBStanding =
                teamBStandings.Entities[0];

            Assert.Equal(
                0,
                teamBStanding.GetAttributeValue<int>(
                    "sol_wins"));

            Assert.Equal(
                1,
                teamBStanding.GetAttributeValue<int>(
                    "sol_losses"));

            Assert.Equal(
                0,
                teamBStanding.GetAttributeValue<int>(
                    "sol_points"));
        }


        // =====================================================
        // TEST 2
        // Final match marks Tournament as Completed
        // =====================================================

        [Fact]
        public void FinalMatch_Should_Mark_Tournament_As_Completed()
        {
            // =================================================
            // ARRANGE
            // =================================================

            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();

            Guid tournamentId = Guid.NewGuid();
            Guid teamAId = Guid.NewGuid();
            Guid teamBId = Guid.NewGuid();
            Guid matchId = Guid.NewGuid();

            // -------------------------------------------------
            // Tournament
            // -------------------------------------------------

            Entity tournament = new Entity("sol_tournament");
            tournament.Id = tournamentId;

            // This is the final match
            tournament["sol_totalmatches"] = 1;
            tournament["sol_completedmatches"] = 0;

            // In Progress
            tournament["sol_status"] =
                new OptionSetValue(2);

            // -------------------------------------------------
            // Match
            // -------------------------------------------------

            Entity match = new Entity("sol_match");
            match.Id = matchId;

            match["sol_tournament"] =
                new EntityReference(
                    "sol_tournament",
                    tournamentId);

            match["sol_teama"] =
                new EntityReference(
                    "sol_team",
                    teamAId);

            match["sol_teamb"] =
                new EntityReference(
                    "sol_team",
                    teamBId);

            context.Initialize(new[]
            {
                tournament,
                match
            });

            // =================================================
            // PRE IMAGE
            // =================================================

            Entity preImage = new Entity("sol_match");
            preImage.Id = matchId;

            preImage["sol_tournament"] =
                new EntityReference(
                    "sol_tournament",
                    tournamentId);

            preImage["sol_teama"] =
                new EntityReference(
                    "sol_team",
                    teamAId);

            preImage["sol_teamb"] =
                new EntityReference(
                    "sol_team",
                    teamBId);

            // No previous winner
            preImage["sol_winner"] = null;

            // =================================================
            // TARGET
            // =================================================

            Entity target = new Entity("sol_match");
            target.Id = matchId;

            // Team A wins
            target["sol_winner"] =
                new EntityReference(
                    "sol_team",
                    teamAId);

            // =================================================
            // ACT
            // =================================================

            var plugin =
                new MatchResultUpdatePlugin.Plugins.MatchResultUpdatePlugin();

            var pluginContext =
                context.GetDefaultPluginContext();

            pluginContext.MessageName = "Update";
            pluginContext.PrimaryEntityName = "sol_match";

            pluginContext.InputParameters["Target"] =
                target;

            pluginContext.PreEntityImages["PreImage"] =
                preImage;

            context.ExecutePluginWith(
                pluginContext,
                plugin);

            // =================================================
            // ASSERT
            // =================================================

            Entity updatedTournament =
                service.Retrieve(
                    "sol_tournament",
                    tournamentId,
                    new ColumnSet(
                        "sol_totalmatches",
                        "sol_completedmatches",
                        "sol_status"));

            // Completed matches = total matches
            Assert.Equal(
                1,
                updatedTournament.GetAttributeValue<int>(
                    "sol_completedmatches"));

            // Completed = 3
            Assert.Equal(
                3,
                updatedTournament
                    .GetAttributeValue<OptionSetValue>(
                        "sol_status")
                    .Value);
        }
    }
}