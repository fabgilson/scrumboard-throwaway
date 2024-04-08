#!/bin/bash

get_pipelines()
{
  status=$1
  pipelines=$(curl --silent --noproxy '*' --header "PRIVATE-TOKEN:$PROJ_ACCESS_TOKEN" "$CI_API_V4_URL/projects/$CI_PROJECT_ID/pipelines?status=$status")
  echo $pipelines
}

get_pipelines_count()
{
  pipelines=$(get_pipelines running)
  active_count=$(echo $pipelines | jq '. | length')
  pending_jobs=$(get_pending_jobs_count)
  result=$(($active_count+$pending_jobs))
  echo $result
}

get_pending_jobs_count()
{
  pending_pipelines=$(get_pipelines pending)

  jobs_count=0
  echo "$pending_pipelines" | jq '.[].id' | while read -r pipeline_id; do
    finished_jobs_len=$(curl --silent --noproxy '*' --header "PRIVATE-TOKEN:$PROJ_ACCESS_TOKEN" "$CI_API_V4_URL/projects/$CI_PROJECT_ID/pipelines/$pipeline_id/jobs?scope[]=failed&scope[]=success" | jq '. | length')
    if [ $finished_jobs_len -gt 0 ]; then ((jobs_count=jobs_count+1)); fi
  done

  echo $jobs_count
}

start_time=$(date +%s)
pipelines_count=$(get_pipelines_count)

printf "Currently $pipelines_count active pipeline$([ $pipelines_count -gt 1 ] && echo "s were" || echo " was") found\n"
until [ $pipelines_count -eq 1 ]
do
  printf '.'
  pipelines_count=$(get_pipelines_count)
  sleep 5
done

end_time=$(date +%s)
total_time=$(( end_time - start_time ))
minutes=$((total_time / 60))
seconds=$((total_time - 60*minutes))
final_time=$(echo "$([ $minutes -ne 0 ] && echo "$minutes minutes, " || echo "")$seconds seconds")
printf "\nLooks like it's only 1 active pipeline at this moment. Proceeding after $final_time\n"
