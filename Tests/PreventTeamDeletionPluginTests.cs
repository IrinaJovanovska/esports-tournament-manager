using System;
using FakeXrmEasy;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using TournamentPlugin.Plugins;
using Xunit;

namespace MatchResultUpdatePlugin.Tests
{
    public class PreventTeamDeletionPluginTests
    {
        // =====================================================
        // TEST 1
        // Team has players -> deletion must be blocked
        // =====================================================

        [Fact]
        public void Team_WithPlayers_Should_Block_Deletion()
        {
            // =================================================
            // ARRANGE
            // =================================================

            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();

            Guid teamId = Guid.NewGuid();
            Guid playerId = Guid.NewGuid();

            // -------------------------------------------------
            // Team
            // -------------------------------------------------

            Entity team = new Entity("sol_team");
            team.Id = teamId;

            team["sol_name"] = "Test Team";

            // -------------------------------------------------
            // Player belonging to the Team
            // -------------------------------------------------

            Entity player = new Entity("sol_player");
            player.Id = playerId;

            player["sol_team"] =
                new EntityReference(
                    "sol_team",
                    teamId);

            context.Initialize(new[]
            {
                team,
                player
            });

            // =================================================
            // DELETE CONTEXT
            // =================================================

            var pluginContext =
                context.GetDefaultPluginContext();

            pluginContext.MessageName = "Delete";
            pluginContext.PrimaryEntityName = "sol_team";

            pluginContext.InputParameters["Target"] =
                new EntityReference(
                    "sol_team",
                    teamId);

            // =================================================
            // ACT + ASSERT
            // =================================================

            var plugin =
                new PreventTeamDeletionPlugin();

            var exception = Assert.Throws<InvalidPluginExecutionException>(
                () =>
                {
                    context.ExecutePluginWith(
                        pluginContext,
                        plugin);
                });

            // =================================================
            // VERIFY ERROR MESSAGE
            // =================================================

            Assert.Contains(
                "Cannot delete this Team because it has registered players",
                exception.Message);

            // =================================================
            // VERIFY TEAM STILL EXISTS
            // =================================================

            Entity existingTeam =
                service.Retrieve(
                    "sol_team",
                    teamId,
                    new ColumnSet(false));

            Assert.NotNull(existingTeam);
        }


        // =====================================================
        // TEST 2
        // Team has no players -> deletion must be allowed
        // =====================================================

        [Fact]
        public void Team_WithoutPlayers_Should_Allow_Deletion()
        {
            // =================================================
            // ARRANGE
            // =================================================

            var context = new XrmFakedContext();

            var service = context.GetOrganizationService();

            Guid teamId = Guid.NewGuid();

            // -------------------------------------------------
            // Team without players
            // -------------------------------------------------

            Entity team = new Entity("sol_team");
            team.Id = teamId;

            team["sol_name"] = "Empty Team";

            context.Initialize(new[]
            {
                team
            });

            // =================================================
            // DELETE CONTEXT
            // =================================================

            var pluginContext =
                context.GetDefaultPluginContext();

            pluginContext.MessageName = "Delete";
            pluginContext.PrimaryEntityName = "sol_team";

            pluginContext.InputParameters["Target"] =
                new EntityReference(
                    "sol_team",
                    teamId);

            // =================================================
            // ACT
            // =================================================

            var plugin =
                new PreventTeamDeletionPlugin();

            // Should NOT throw an exception
            context.ExecutePluginWith(
                pluginContext,
                plugin);

            // =================================================
            // ASSERT
            // =================================================

            Entity existingTeam =
                service.Retrieve(
                    "sol_team",
                    teamId,
                    new ColumnSet(false));

            Assert.NotNull(existingTeam);
        }
    }
}