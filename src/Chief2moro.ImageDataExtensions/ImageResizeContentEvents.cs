﻿using System;
using System.Reflection;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAnnotations;
using EPiServer.Framework.Blobs;
using EPiServer.ServiceLocation;

namespace Chief2moro.ImageDataExtensions
{
    public class ImageResizeContentEvents
    {
        public void contentEvents_PublishingContent(object sender, ContentEventArgs e)
        {
            var content = e.Content;
            if (!(content is ImageData)) 
                return;
            
            var image = content as ImageData;
            Dimensions imageDimensions = ImageBlobUtility.GetDimensions(image.BinaryData);
            
            PropertyInfo[] properties = image.GetType().GetProperties();
            int requiredWidth = imageDimensions.Width;

            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.PropertyType != typeof(Blob))
                    continue;

                //get attribute name
                var imageWidthAttribute = (ImageWidthDescriptorAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(ImageWidthDescriptorAttribute));
                var imageScaleAttribute = (ImageScaleDescriptorAttribute)Attribute.GetCustomAttribute(propertyInfo, typeof(ImageScaleDescriptorAttribute));

                if (imageWidthAttribute != null)
                {
                    requiredWidth = imageWidthAttribute.Width;
                }
                else if (imageScaleAttribute != null)
                {
                    requiredWidth = (int) (imageDimensions.Width*imageScaleAttribute.DimensionMultiplier);
                }
                else
                {
                    continue;
                }

                var calculatedDimensions = ImageResizeUtility.ResizeWidthMaintainAspectRatio(imageDimensions, requiredWidth);  
                var imageDescriptor = new ImageDescriptorAttribute(calculatedDimensions.Height, calculatedDimensions.Width);

                var thumbnailManager = ServiceLocator.Current.GetInstance<ThumbnailManager>();
                var resizedBlob = thumbnailManager.CreateImageBlob(image.BinaryData, propertyInfo.Name, imageDescriptor);

                //assign blob to current property
                propertyInfo.SetValue(image, resizedBlob, null);
            }
        }
    }
}