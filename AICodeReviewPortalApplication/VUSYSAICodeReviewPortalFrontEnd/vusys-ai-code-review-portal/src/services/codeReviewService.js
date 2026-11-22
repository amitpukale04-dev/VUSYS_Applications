import apiClient from "./apiClient";
export const listRepositories=async(options)=> (await apiClient.get("/repositories",options)).data;
export const listPullRequests=async(owner,repo,options)=> (await apiClient.get(`/repositories/${encodeURIComponent(owner)}/${encodeURIComponent(repo)}/pulls`,options)).data;
export const listReviews=async()=> (await apiClient.get("/reviews")).data;
export const getReview=async(id)=> (await apiClient.get(`/reviews/${id}`)).data;
export const analyzePullRequest=async(owner,repo,number)=> (await apiClient.post("/reviews/analyze",{owner,repo,number})).data;
export const publishReview=async(reviewId,owner,repo,number)=> (await apiClient.post("/reviews/publish",{reviewId,owner,repo,number})).data;

const pullRequestPath = (owner, repo, number) =>
  `/PullRequests/${encodeURIComponent(owner)}/${encodeURIComponent(repo)}/${number}`;

export const getPullRequestFiles = async (owner, repo, number, signal) =>
  (await apiClient.get(`${pullRequestPath(owner, repo, number)}/Files`, { signal, skipGlobalErrorToast: true })).data;

export const getPullRequestFileContent = async (owner, repo, number, fileName, signal) =>
  (await apiClient.get(`${pullRequestPath(owner, repo, number)}/FileContent`, {
    params: { fileName },
    skipGlobalErrorToast: true,
    signal,
  })).data;

export const publishManualReview = async (review) =>
  (await apiClient.post("/reviews/manual/publish", review, { skipGlobalErrorToast: true })).data;
