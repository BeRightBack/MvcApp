using MvcApp.Core;

namespace MvcApp.Module.Pages.Services;

/// <summary>
/// Built-in snippet catalog. Seeded into the DB once so admins can extend or
/// edit the premade blocks offered in the page editor's snippet picker.
/// </summary>
public static class SnippetCatalog
{
    public static readonly List<ContentPageSnippet> Defaults = Build();

    private static List<ContentPageSnippet> Build() =>
    [
        new() { Name = "Hero / Jumbotron", Category = "Layout", IsSystem = true,
                Description = "Full-width hero with title, subtitle and two buttons.",
                Content = "<div class=\"p-5 mb-4 bg-light rounded-3 text-center\">\n  <h1 class=\"display-5 fw-bold\">Your headline here</h1>\n  <p class=\"lead\">Supporting text that explains what this page is about.</p>\n  <a class=\"btn btn-primary btn-lg\" href=\"#\">Primary action</a>\n  <a class=\"btn btn-outline-secondary btn-lg\" href=\"#\">Secondary</a>\n</div>" },
        new() { Name = "Feature cards (3-col)", Category = "Layout", IsSystem = true,
                Description = "Three columns with icon, heading and text.",
                Content = "<div class=\"row g-4\">\n  <div class=\"col-md-4\">\n    <div class=\"card h-100\"><div class=\"card-body\">\n      <i class='bx bxs-cog text-primary fs-3'></i>\n      <h5 class=\"card-title mt-2\">Feature one</h5>\n      <p class=\"card-text\">Describe the first feature here.</p>\n    </div></div>\n  </div>\n  <div class=\"col-md-4\">\n    <div class=\"card h-100\"><div class=\"card-body\">\n      <i class='bx bxs-bolt text-success fs-3'></i>\n      <h5 class=\"card-title mt-2\">Feature two</h5>\n      <p class=\"card-text\">Describe the second feature here.</p>\n    </div></div>\n  </div>\n  <div class=\"col-md-4\">\n    <div class=\"card h-100\"><div class=\"card-body\">\n      <i class='bx bxs-shield text-warning fs-3'></i>\n      <h5 class=\"card-title mt-2\">Feature three</h5>\n      <p class=\"card-text\">Describe the third feature here.</p>\n    </div></div>\n  </div>\n</div>" },
        new() { Name = "Call-to-action band", Category = "Layout", IsSystem = true,
                Description = "Wide coloured band with heading and a button.",
                Content = "<div class=\"p-5 mb-4 bg-primary text-white rounded-3 text-center\">\n  <h2 class=\"fw-bold\">Ready to get started?</h2>\n  <p class=\"mb-4\">Short pitch encouraging the visitor to act.</p>\n  <a class=\"btn btn-light btn-lg\" href=\"#\">Get started</a>\n</div>" },
        new() { Name = "Two-column split", Category = "Layout", IsSystem = true,
                Description = "Text beside an image in a 2-column layout.",
                Content = "<div class=\"row align-items-center g-4\">\n  <div class=\"col-md-6\">\n    <h2>Left column heading</h2>\n    <p>Paragraph of text for the left column.</p>\n  </div>\n  <div class=\"col-md-6\">\n    <img src=\"https://picsum.photos/600/400\" class=\"img-fluid rounded\" alt=\"Placeholder\" />\n  </div>\n</div>" },
        new() { Name = "Alert box", Category = "Components", IsSystem = true,
                Description = "Bootstrap alert with a dismiss button.",
                Content = "<div class=\"alert alert-info alert-dismissible fade show\" role=\"alert\">\n  Your alert message here.\n  <button type=\"button\" class=\"btn-close\" data-bs-dismiss=\"alert\"></button>\n</div>" },
        new() { Name = "Info callout", Category = "Components", IsSystem = true,
                Description = "Emphasised callout with icon and heading.",
                Content = "<div class=\"d-flex gap-3 p-3 mb-3 border-start border-4 border-primary bg-light\">\n  <i class='bx bxs-info-circle fs-3 text-primary'></i>\n  <div>\n    <h5 class=\"mb-1\">Did you know?</h5>\n    <p class=\"mb-0\">Add the useful note here.</p>\n  </div>\n</div>" },
        new() { Name = "Accordion (FAQ)", Category = "Components", IsSystem = true,
                Description = "Collapsible Q&A accordion.",
                Content = "<div class=\"accordion mb-4\" id=\"faqAccordion\">\n  <div class=\"accordion-item\">\n    <h2 class=\"accordion-header\"><button class=\"accordion-button\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#faq1\">Question one?</button></h2>\n    <div id=\"faq1\" class=\"accordion-collapse collapse show\" data-bs-parent=\"#faqAccordion\"><div class=\"accordion-body\">Answer one.</div></div>\n  </div>\n  <div class=\"accordion-item\">\n    <h2 class=\"accordion-header\"><button class=\"accordion-button collapsed\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#faq2\">Question two?</button></h2>\n    <div id=\"faq2\" class=\"accordion-collapse collapse\" data-bs-parent=\"#faqAccordion\"><div class=\"accordion-body\">Answer two.</div></div>\n  </div>\n</div>" },
        new() { Name = "Embedded video", Category = "Media", IsSystem = true,
                Description = "Responsive 16:9 video embed (YouTube/Vimeo).",
                Content = "<div class=\"ratio ratio-16x9 mb-4\">\n  <iframe src=\"https://www.youtube.com/embed/dQw4w9WgXcQ\" title=\"Video\" allowfullscreen></iframe>\n</div>" },
        new() { Name = "Image with caption", Category = "Media", IsSystem = true,
                Description = "Responsive image wrapped in a figure with caption.",
                Content = "<figure class=\"text-center\">\n  <img src=\"https://picsum.photos/800/450\" class=\"img-fluid rounded\" alt=\"Caption\" />\n  <figcaption class=\"text-muted small mt-2\">Caption describing the image.</figcaption>\n</figure>" },
        new() { Name = "Pricing table", Category = "Content", IsSystem = true,
                Description = "Three-column pricing comparison.",
                Content = "<div class=\"row g-4\">\n  <div class=\"col-md-4\"><div class=\"card h-100\"><div class=\"card-body text-center\"><h5>Basic</h5><h2 class=\"fw-bold\">$9</h2><p class=\"text-muted\">/month</p><ul class=\"list-unstyled mb-3\"><li>Feature A</li><li>Feature B</li></ul><a class=\"btn btn-outline-primary w-100\" href=\"#\">Choose</a></div></div></div>\n  <div class=\"col-md-4\"><div class=\"card h-100 border-primary\"><div class=\"card-body text-center\"><h5>Pro</h5><h2 class=\"fw-bold\">$19</h2><p class=\"text-muted\">/month</p><ul class=\"list-unstyled mb-3\"><li>Feature A</li><li>Feature B</li><li>Feature C</li></ul><a class=\"btn btn-primary w-100\" href=\"#\">Choose</a></div></div></div>\n  <div class=\"col-md-4\"><div class=\"card h-100\"><div class=\"card-body text-center\"><h5>Team</h5><h2 class=\"fw-bold\">$49</h2><p class=\"text-muted\">/month</p><ul class=\"list-unstyled mb-3\"><li>Everything</li></ul><a class=\"btn btn-outline-primary w-100\" href=\"#\">Choose</a></div></div></div>\n</div>" },
        new() { Name = "Stats row", Category = "Content", IsSystem = true,
                Description = "Row of big numbers with labels.",
                Content = "<div class=\"row text-center g-4\">\n  <div class=\"col-md-3\"><div class=\"display-4 fw-bold text-primary\">1.2k</div><p class=\"text-muted\">Members</p></div>\n  <div class=\"col-md-3\"><div class=\"display-4 fw-bold text-primary\">340</div><p class=\"text-muted\">Projects</p></div>\n  <div class=\"col-md-3\"><div class=\"display-4 fw-bold text-primary\">99%</div><p class=\"text-muted\">Uptime</p></div>\n  <div class=\"col-md-3\"><div class=\"display-4 fw-bold text-primary\">24/7</div><p class=\"text-muted\">Support</p></div>\n</div>" },
        new() { Name = "Contact info cards", Category = "Content", IsSystem = true,
                Description = "Contact details in icon cards.",
                Content = "<div class=\"row g-4\">\n  <div class=\"col-md-4\"><div class=\"card h-100 text-center\"><div class=\"card-body\"><i class='bx bxs-envelope fs-2 text-primary'></i><h6 class=\"mt-2\">Email</h6><p class=\"mb-0\">hello@example.com</p></div></div></div>\n  <div class=\"col-md-4\"><div class=\"card h-100 text-center\"><div class=\"card-body\"><i class='bx bxs-phone fs-2 text-success'></i><h6 class=\"mt-2\">Phone</h6><p class=\"mb-0\">+1 555 000 0000</p></div></div></div>\n  <div class=\"col-md-4\"><div class=\"card h-100 text-center\"><div class=\"card-body\"><i class='bx bxs-map fs-2 text-danger'></i><h6 class=\"mt-2\">Address</h6><p class=\"mb-0\">123 Main Street</p></div></div></div>\n</div>" },
        new() { Name = "Blockquote", Category = "Components", IsSystem = true,
                Description = "Styled testimonial quote.",
                Content = "<blockquote class=\"blockquote text-center py-4\">\n  <p class=\"fs-4 fst-italic\">\"This product changed the way we work.\"</p>\n  <footer class=\"blockquote-footer\">Jane Doe, Acme Inc.</footer>\n</blockquote>" },
        new() { Name = "Timeline", Category = "Content", IsSystem = true,
                Description = "Vertical timeline of milestones.",
                Content = "<ul class=\"list-unstyled\">\n  <li class=\"mb-4\"><strong>2023</strong><p class=\"mb-0\">Milestone one.</p></li>\n  <li class=\"mb-4\"><strong>2024</strong><p class=\"mb-0\">Milestone two.</p></li>\n  <li class=\"mb-4\"><strong>2025</strong><p class=\"mb-0\">Milestone three.</p></li>\n</ul>" }
    ];
}
