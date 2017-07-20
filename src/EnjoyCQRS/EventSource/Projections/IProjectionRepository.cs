﻿// The MIT License (MIT)
// 
// Copyright (c) 2016 Nelson Corrêa V. Júnior
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EnjoyCQRS.EventSource.Projections
{
    public interface IProjectionRepository
    {
        /// <summary>
        /// Get the projection based on category and id.
        /// </summary>
        /// <param name="projectionType"></param>
        /// <param name="category"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<object> GetAsync(Type projectionType, string category, Guid id);
        
        /// <summary>
        /// <para>
        /// Get the projection based on projection name (i.e. PersonAggregate-a5b4fd92-fcfa-4c50-b626-109c3e6d8967),
        /// </para>
        /// where PersonAggregate is the category of projection and a5b4fd92-fcfa-4c50-b626-109c3e6d8967 is the aggregate id.
        /// </summary>
        /// <param name="projectionType"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<object> GetAsync(Type projectionType, Guid id);
    }

    public interface IProjectionRepository<TProjection> : IProjectionRepository
    {
        /// <summary>
        /// Get the projection based on category and id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TProjection> GetAsync(Guid id);
        
        /// <summary>
        /// Find projection based on expression.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        Task<IEnumerable<TProjection>> FindAsync(string category, Expression<Func<TProjection, bool>> expr);

        /// <summary>
        /// Find projection based on expression.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="expr"></param>
        /// <returns></returns>
        Task<IEnumerable<TProjection>> FindAsync(Expression<Func<TProjection, bool>> expr);
    }
}
