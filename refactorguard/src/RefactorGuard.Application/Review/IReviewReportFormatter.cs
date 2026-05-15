namespace RefactorGuard.Application.Review;

public interface IReviewReportFormatter
{
    string Format(DiffReviewReport report);
}
