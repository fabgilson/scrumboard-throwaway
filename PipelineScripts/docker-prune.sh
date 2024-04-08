#!/bin/bash

# Check if repository name is provided
if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <repository_name>"
    exit 1
fi

REPO_NAME=$1
KEEP_LAST_N=5

# Get unique image names in the repository
IMAGE_NAMES=$(docker images --format '{{.Repository}}' | grep "$REPO_NAME" | uniq)

for image in $IMAGE_NAMES; do
    # Delete old tags for each image, keeping the latest N
    OLD_TAGS=$(docker images --format '{{.Tag}}' "$image" | tail -n +$((KEEP_LAST_N+1)))
    
    for tag in $OLD_TAGS; do
        echo "Removing tag $tag from image $image"
        docker rmi "$image:$tag"
    done
done

# Remove unused images excluding those from the specified repository
UNUSED_IMAGES=$(docker images -a -q | xargs -n 1 docker image inspect --format '{{.Id}} {{.RepoTags}}' | grep -v "$REPO_NAME" | awk '{print $1}')

for image_id in $UNUSED_IMAGES; do
    echo "Removing image $image_id"
    docker rmi -f "$image_id"
done

# If there are more than 3 images for the specified repository, get all but the 3 most recent image IDs
EXCESS_IMAGES_FROM_REPO=$(docker image ls "$REPO_NAME" --format '{{.CreatedAt}},{{.ID}}' | sort -r | tail -n +4 | awk -F ',' '{print $2}')

# Count total images
TOTAL_IMAGES=$(docker image ls -q | wc -l)

# Count unused images
UNUSED_IMAGES_COUNT=$(echo "$UNUSED_IMAGES" | grep -c .)

# Count images from the specified repository that will be removed
IMAGES_TO_BE_REMOVED_FROM_REPO=$(echo "$EXCESS_IMAGES_FROM_REPO" | grep -c .)

# Display counts
echo "Total images: $TOTAL_IMAGES"
echo "Unused images to be removed: $UNUSED_IMAGES_COUNT"
echo "Images from '$REPO_NAME' to be removed: $IMAGES_TO_BE_REMOVED_FROM_REPO"

# Remove excess images from the specified repository
echo "$EXCESS_IMAGES_FROM_REPO" | xargs -r docker rmi -f
