using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

internal sealed class ResourceBindingRegistry
{
    private static readonly IReadOnlyDictionary<ResourceKind, IReadOnlySet<string>> Scopes =
        new Dictionary<ResourceKind, IReadOnlySet<string>>
        {
            [ResourceKind.Application] = new HashSet<string>(["Summary", "Proposal"], StringComparer.Ordinal),
            [ResourceKind.Booking] = new HashSet<string>(["Summary", "Operations"], StringComparer.Ordinal),
            [ResourceKind.Contract] = new HashSet<string>(["Read"], StringComparer.Ordinal),
            [ResourceKind.Concert] = new HashSet<string>(["Summary", "Operations", "Finance"], StringComparer.Ordinal),
            [ResourceKind.Invoice] = new HashSet<string>(["Read"], StringComparer.Ordinal),
            [ResourceKind.Conversation] = new HashSet<string>(["Read", "SendMessages"], StringComparer.Ordinal),
        };

    private static readonly IReadOnlyDictionary<ResourceKind, IReadOnlySet<string>> Policies =
        new Dictionary<ResourceKind, IReadOnlySet<string>>
        {
            [ResourceKind.Application] = new HashSet<string>(
                ["application_grant", "venue_principal", "artist_principal"], StringComparer.Ordinal),
            [ResourceKind.Booking] = new HashSet<string>(
                ["booking_grant", "either_principal"], StringComparer.Ordinal),
            [ResourceKind.Contract] = new HashSet<string>(["contract_grant"], StringComparer.Ordinal),
            [ResourceKind.Concert] = new HashSet<string>(
                ["concert_grant", "venue_principal", "either_principal", "principal_administration"],
                StringComparer.Ordinal),
            [ResourceKind.Invoice] = new HashSet<string>(["invoice_grant"], StringComparer.Ordinal),
            [ResourceKind.Conversation] = new HashSet<string>(
                ["conversation_grant", "principal_administration"], StringComparer.Ordinal),
        };

    private readonly IReadOnlyDictionary<(TenantPermission, ResourceKind), ImmutableArray<ResourcePolicyBinding>> bindings;
    private readonly IReadOnlyDictionary<ResourceKind, IResourceAuthorizationEvaluator> evaluators;

    public ResourceBindingRegistry(
        IEnumerable<PermissionDescriptor> descriptors,
        IEnumerable<IResourceAuthorizationEvaluator> evaluators)
    {
        this.evaluators = evaluators.ToDictionary(evaluator => evaluator.Kind);
        var entries = new List<ResourcePolicyBinding>();
        foreach (var descriptor in descriptors)
        {
            if (!TenantPermission.All.Contains(descriptor.Permission))
                throw new InvalidOperationException("An unknown permission has a resource binding.");

            foreach (var source in descriptor.ResourceBindings.Where(binding => binding.Resource != "tenant"))
            {
                if (!Enum.TryParse<ResourceKind>(source.Resource, true, out var kind)
                    || !Enum.IsDefined(kind)
                    || !this.evaluators.ContainsKey(kind)
                    || !Policies.GetValueOrDefault(kind, new HashSet<string>()).Contains(source.Policy)
                    || (source.Facet is not null
                        && (!Enum.TryParse<ResourceFacet>(source.Facet, false, out var facet)
                            || !Enum.IsDefined(facet)))
                    || source.RequiresScopes is null
                    || source.RequiresScopes.Any(scope => !Scopes.GetValueOrDefault(kind, new HashSet<string>()).Contains(scope))
                    || source.RequiresScopes.Count != source.RequiresScopes.Distinct(StringComparer.Ordinal).Count())
                    throw new InvalidOperationException("An invalid resource binding is registered.");

                ResourceFacet? parsedFacet = source.Facet is null
                    ? null
                    : Enum.Parse<ResourceFacet>(source.Facet, false);
                entries.Add(new ResourcePolicyBinding(descriptor.Permission, kind, parsedFacet,
                    source.Policy, source.RequiresScopes.ToImmutableArray()));
            }
        }

        if (entries.Count != entries.Select(entry => (entry.Permission, entry.Resource, entry.Facet)).Distinct().Count())
            throw new InvalidOperationException("Duplicate resource bindings are registered.");

        bindings = entries.GroupBy(entry => (entry.Permission, entry.Resource))
            .ToDictionary(group => group.Key, group => group.ToImmutableArray());
    }

    public bool TryResolve(AuthorizationRequest request, out ResourcePolicyBinding? binding)
    {
        binding = null;
        if (!bindings.TryGetValue((request.Permission, request.Resource.Kind), out var candidates))
            return false;

        if (request.Facet is { } facet)
        {
            binding = candidates.SingleOrDefault(candidate => candidate.Facet == facet);
            return binding is not null;
        }

        if (candidates.Length != 1)
            return false;

        binding = candidates[0];
        return true;
    }

    public IResourceAuthorizationEvaluator Evaluator(ResourceKind kind) => evaluators[kind];
}
